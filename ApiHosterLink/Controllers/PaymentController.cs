using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IMongoCollection<Payment> _payments;

        public PaymentController(IMongoDatabase database)
        {
            _payments = database.GetCollection<Payment>("Payments");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            var payments = await _payments.Find(_ => true).ToListAsync();
            return Ok(payments);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByUser(string userId)
        {
            var payments = await _payments.Find(p => p.UserId == userId).ToListAsync();
            return Ok(payments);
        }

        [HttpGet("pending/{userId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPendingPayments(string userId)
        {
            var payments = await _payments.Find(p => p.UserId == userId && p.Status == "Pending").ToListAsync();
            return Ok(payments);
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
        {
            payment.PaidDate = null; // Se establecerá cuando se complete
            await _payments.InsertOneAsync(payment);
            return CreatedAtAction(nameof(GetPayments), new { id = payment.Id }, payment);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdatePaymentStatus(string id, [FromBody] string status)
        {
            var update = Builders<Payment>.Update
                .Set(p => p.Status, status)
                .Set(p => p.PaidDate, status == "Completed" ? DateTime.UtcNow : null);

            var result = await _payments.UpdateOneAsync(p => p.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpGet("report/{propertyId}/{month}/{year}")]
        public async Task<ActionResult<object>> GetPaymentReport(string propertyId, int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var payments = await _payments.Find(p =>
                p.PropertyId == propertyId &&
                p.DueDate >= startDate &&
                p.DueDate < endDate
            ).ToListAsync();

            var total = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount);
            var pending = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount);

            return Ok(new { TotalCollected = total, PendingAmount = pending, Payments = payments });
        }
    }
}
