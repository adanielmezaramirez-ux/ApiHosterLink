using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todos requieren autenticación
    public class PaymentController : ControllerBase
    {
        private readonly IMongoCollection<Payment> _payments;

        public PaymentController(IMongoDatabase database)
        {
            _payments = database.GetCollection<Payment>("Payments");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")] // Solo admins ven todos los pagos
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            var payments = await _payments.Find(_ => true).ToListAsync();
            return Ok(payments);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByUser(string userId)
        {
            if (!IsValidObjectId(userId))
                return BadRequest("ID de usuario no válido");

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Solo puede ver sus propios pagos o si es Admin
            if (userRole != "Admin" && userId != currentUserId)
                return Forbid();

            var filter = Builders<Payment>.Filter.Eq(p => p.UserId, userId);
            var payments = await _payments.Find(filter).ToListAsync();
            return Ok(payments);
        }

        [HttpGet("pending/{userId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPendingPayments(string userId)
        {
            if (!IsValidObjectId(userId))
                return BadRequest("ID de usuario no válido");

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userId != currentUserId)
                return Forbid();

            var filter = Builders<Payment>.Filter.Eq(p => p.UserId, userId) &
                         Builders<Payment>.Filter.Eq(p => p.Status, "Pending");
            var payments = await _payments.Find(filter).ToListAsync();
            return Ok(payments);
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
        {
            // Validaciones
            if (payment.Amount <= 0)
                return BadRequest("El monto debe ser mayor a 0");

            if (string.IsNullOrEmpty(payment.PaymentType))
                return BadRequest("El tipo de pago es requerido");

            if (string.IsNullOrEmpty(payment.PaymentMethod))
                return BadRequest("El método de pago es requerido");

            // Validar tipos y métodos
            var validPaymentTypes = new[] { "Rent", "Maintenance", "Service" };
            var validPaymentMethods = new[] { "CreditCard", "DebitCard", "Cash", "Transfer" };

            if (!validPaymentTypes.Contains(payment.PaymentType))
                return BadRequest("Tipo de pago no válido");

            if (!validPaymentMethods.Contains(payment.PaymentMethod))
                return BadRequest("Método de pago no válido");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Asignar usuario actual
            payment.UserId = userId;
            payment.PaidDate = null;
            payment.Status = "Pending";

            // Validar fecha de vencimiento
            if (payment.DueDate < DateTime.UtcNow.Date)
                return BadRequest("La fecha de vencimiento no puede ser en el pasado");

            await _payments.InsertOneAsync(payment);
            return CreatedAtAction(nameof(GetPayments), new { id = payment.Id }, payment);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Owner")] // Solo admins y owners pueden cambiar estado
        public async Task<IActionResult> UpdatePaymentStatus(string id, [FromBody] string status)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de pago no válido");

            // Validar estado
            var validStatuses = new[] { "Pending", "Completed", "Failed", "Refunded" };
            if (!validStatuses.Contains(status))
                return BadRequest("Estado no válido");

            var payment = await _payments.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (payment == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                // Owners solo pueden cambiar estado de pagos de sus propiedades
                // Necesitaríamos verificar si el owner es admin de la propiedad
                return Forbid();
            }

            var update = Builders<Payment>.Update
                .Set(p => p.Status, status)
                .Set(p => p.PaidDate, status == "Completed" ? DateTime.UtcNow : (DateTime?)null);

            var result = await _payments.UpdateOneAsync(p => p.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpGet("report/{propertyId}/{month}/{year}")]
        public async Task<ActionResult<object>> GetPaymentReport(string propertyId, int month, int year)
        {
            if (!IsValidObjectId(propertyId))
                return BadRequest("ID de propiedad no válido");

            // Validar mes y año
            if (month < 1 || month > 12)
                return BadRequest("Mes debe estar entre 1 y 12");

            if (year < 2000 || year > 2100)
                return BadRequest("Año no válido");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Solo admins y owners de la propiedad pueden ver reportes
            if (userRole != "Admin")
            {
                // Verificar si el usuario es owner de la propiedad
                // Esto requeriría una consulta adicional a la colección de propiedades
                return Forbid();
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var filter = Builders<Payment>.Filter.Eq(p => p.PropertyId, propertyId) &
                         Builders<Payment>.Filter.Gte(p => p.DueDate, startDate) &
                         Builders<Payment>.Filter.Lt(p => p.DueDate, endDate);

            var payments = await _payments.Find(filter).ToListAsync();

            var total = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount);
            var pending = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount);

            return Ok(new
            {
                TotalCollected = total,
                PendingAmount = pending,
                Payments = payments,
                Period = $"{month}/{year}",
                PropertyId = propertyId
            });
        }

        private bool IsValidObjectId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 24;
        }
    }
}