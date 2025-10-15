using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMongoCollection<MaintenanceRequest> _maintenanceRequests;

        public MaintenanceController(IMongoDatabase database)
        {
            _maintenanceRequests = database.GetCollection<MaintenanceRequest>("MaintenanceRequests");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceRequest>>> GetMaintenanceRequests()
        {
            var requests = await _maintenanceRequests.Find(_ => true).ToListAsync();
            return Ok(requests);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequest>>> GetRequestsByUser(string userId)
        {
            var requests = await _maintenanceRequests.Find(r => r.UserId == userId).ToListAsync();
            return Ok(requests);
        }

        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequest>>> GetRequestsByStatus(string status)
        {
            var requests = await _maintenanceRequests.Find(r => r.Status == status).ToListAsync();
            return Ok(requests);
        }

        [HttpPost]
        public async Task<ActionResult<MaintenanceRequest>> CreateRequest(MaintenanceRequest request)
        {
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.Status = "Pending";
            await _maintenanceRequests.InsertOneAsync(request);
            return CreatedAtAction(nameof(GetMaintenanceRequests), new { id = request.Id }, request);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateRequestStatus(string id, [FromBody] string status)
        {
            var update = Builders<MaintenanceRequest>.Update
                .Set(r => r.Status, status)
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await _maintenanceRequests.UpdateOneAsync(r => r.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/assign")]
        public async Task<IActionResult> AssignMaintenanceStaff(string id, [FromBody] string staffId)
        {
            var update = Builders<MaintenanceRequest>.Update
                .Set(r => r.AssignedTo, staffId)
                .Set(r => r.Status, "InProgress")
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await _maintenanceRequests.UpdateOneAsync(r => r.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/cost")]
        public async Task<IActionResult> UpdateCost(string id, [FromBody] decimal actualCost)
        {
            var update = Builders<MaintenanceRequest>.Update
                .Set(r => r.ActualCost, actualCost)
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await _maintenanceRequests.UpdateOneAsync(r => r.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }
    }
}
