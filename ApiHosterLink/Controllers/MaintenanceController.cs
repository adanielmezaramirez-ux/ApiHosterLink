using System.Collections.Generic;
using System.Threading.Tasks;
using ApiHosterLink.Helpers;
using ApiHosterLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todos requieren autenticación
    public class MaintenanceController : ControllerBase
    {
        private readonly IMongoCollection<MaintenanceRequest> _maintenanceRequests;
        private readonly IValidationService _validationService;

        public MaintenanceController(IMongoDatabase database, IValidationService validationService)
        {
            _maintenanceRequests = database.GetCollection<MaintenanceRequest>("MaintenanceRequests");
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetMaintenanceRequests(int page = 1, int pageSize = 20, string status = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            FilterDefinition<MaintenanceRequest> filter = Builders<MaintenanceRequest>.Filter.Empty;

            if (userRole != "Admin")
            {
                filter = Builders<MaintenanceRequest>.Filter.Eq(r => r.UserId, userId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                filter &= Builders<MaintenanceRequest>.Filter.Eq(r => r.Status, status);
            }

            var (results, total) = await MongoDBHelper.GetPaginatedResults(
                _maintenanceRequests, filter, Builders<MaintenanceRequest>.Sort.Descending(r => r.CreatedAt), page, pageSize);

            return Ok(new
            {
                Requests = results,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequest>>> GetRequestsByUser(string userId)
        {
            if (!IsValidObjectId(userId))
                return BadRequest("ID de usuario no válido");

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Solo puede ver sus propias solicitudes o si es Admin
            if (userRole != "Admin" && userId != currentUserId)
                return Forbid();

            var filter = Builders<MaintenanceRequest>.Filter.Eq(r => r.UserId, userId);
            var requests = await _maintenanceRequests.Find(filter).ToListAsync();
            return Ok(requests);
        }

        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequest>>> GetRequestsByStatus(string status)
        {
            // Validar estado
            var validStatuses = new[] { "Pending", "InProgress", "Completed", "Cancelled" };
            if (!validStatuses.Contains(status))
                return BadRequest("Estado no válido");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            FilterDefinition<MaintenanceRequest> filter;

            if (userRole == "Admin")
            {
                filter = Builders<MaintenanceRequest>.Filter.Eq(r => r.Status, status);
            }
            else
            {
                // Usuarios normales solo ven sus propias solicitudes
                filter = Builders<MaintenanceRequest>.Filter.Eq(r => r.UserId, userId) &
                         Builders<MaintenanceRequest>.Filter.Eq(r => r.Status, status);
            }

            var requests = await _maintenanceRequests.Find(filter).ToListAsync();
            return Ok(requests);
        }

        [HttpPost]
        public async Task<ActionResult<MaintenanceRequest>> CreateRequest(MaintenanceRequest request)
        {
            // Validaciones
            if (string.IsNullOrEmpty(request.Title) || request.Title.Length < 5)
                return BadRequest("El título debe tener al menos 5 caracteres");

            if (string.IsNullOrEmpty(request.Description) || request.Description.Length < 10)
                return BadRequest("La descripción debe tener al menos 10 caracteres");

            if (string.IsNullOrEmpty(request.Priority))
                return BadRequest("La prioridad es requerida");

            // Validar prioridad
            var validPriorities = new[] { "Low", "Medium", "High", "Emergency" };
            if (!validPriorities.Contains(request.Priority))
                return BadRequest("Prioridad no válida");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Asignar usuario actual como creador
            request.UserId = userId;
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.Status = "Pending";

            // Validar que los costos no sean negativos
            if (request.EstimatedCost < 0 || request.ActualCost < 0)
                return BadRequest("Los costos no pueden ser negativos");

            await _maintenanceRequests.InsertOneAsync(request);
            return CreatedAtAction(nameof(GetMaintenanceRequests), new { id = request.Id }, request);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Owner")] // Solo admins y owners pueden cambiar estado
        public async Task<IActionResult> UpdateRequestStatus(string id, [FromBody] string status)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de solicitud no válido");

            // Validar estado
            var validStatuses = new[] { "Pending", "InProgress", "Completed", "Cancelled" };
            if (!validStatuses.Contains(status))
                return BadRequest("Estado no válido");

            var request = await _maintenanceRequests.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (request == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Verificar permisos (Admin o Owner de la propiedad)
            // Nota: Necesitaríamos verificar si el usuario es owner de la propiedad
            if (!User.IsInRole("Admin"))
            {
                // Por ahora, solo permitimos a Admins cambiar estado
                // Podrías agregar lógica para verificar propiedad
                return Forbid();
            }

            var update = Builders<MaintenanceRequest>.Update
                .Set(r => r.Status, status)
                .Set(r => r.UpdatedAt, DateTime.UtcNow)
                .Set(r => r.PaidDate, status == "Completed" ? DateTime.UtcNow : (DateTime?)null);

            var result = await _maintenanceRequests.UpdateOneAsync(r => r.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Admin,Owner")] // Solo admins y owners pueden asignar
        public async Task<IActionResult> AssignMaintenanceStaff(string id, [FromBody] string staffId)
        {
            if (!IsValidObjectId(id) || !IsValidObjectId(staffId))
                return BadRequest("IDs no válidos");

            var request = await _maintenanceRequests.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (request == null) return NotFound();

            // Verificar permisos (similar al método anterior)
            if (!User.IsInRole("Admin"))
                return Forbid();

            var update = Builders<MaintenanceRequest>.Update
                .Set(r => r.AssignedTo, staffId)
                .Set(r => r.Status, "InProgress")
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await _maintenanceRequests.UpdateOneAsync(r => r.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/cost")]
        [Authorize(Roles = "Admin,Owner")] // Solo admins y owners pueden actualizar costos
        public async Task<IActionResult> UpdateCost(string id, [FromBody] decimal actualCost)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de solicitud no válido");

            if (actualCost < 0)
                return BadRequest("El costo no puede ser negativo");

            var request = await _maintenanceRequests.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (request == null) return NotFound();

            if (!User.IsInRole("Admin"))
                return Forbid();

            var update = Builders<MaintenanceRequest>.Update
                .Set(r => r.ActualCost, actualCost)
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await _maintenanceRequests.UpdateOneAsync(r => r.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        private bool IsValidObjectId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 24;
        }
    }
}