using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnitController : ControllerBase
    {
        private readonly IMongoCollection<Property> _properties;

        public UnitController(IMongoDatabase database)
        {
            _properties = database.GetCollection<Property>("Properties");
        }

        [HttpGet("by-property/{propertyId}")]
        public async Task<ActionResult<IEnumerable<Unit>>> GetUnitsByProperty(string propertyId)
        {
            var property = await _properties.Find(p => p.Id == propertyId && p.IsActive)
                .Project(p => new { p.Units })
                .FirstOrDefaultAsync();

            if (property == null) return NotFound();
            return Ok(property.Units.Where(u => u.IsOccupied || !u.IsOccupied)); // Mostrar todas
        }

        [HttpPut("assign-tenant/{propertyId}/{unitId}")]
        public async Task<IActionResult> AssignTenant(string propertyId, string unitId, [FromBody] string tenantId)
        {
            var filter = Builders<Property>.Filter.And(
                Builders<Property>.Filter.Eq(p => p.Id, propertyId),
                Builders<Property>.Filter.ElemMatch(p => p.Units, u => u.Id == unitId)
            );

            var update = Builders<Property>.Update
                .Set(p => p.Units[-1].TenantId, tenantId)
                .Set(p => p.Units[-1].IsOccupied, true);

            var result = await _properties.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpPut("remove-tenant/{propertyId}/{unitId}")]
        public async Task<IActionResult> RemoveTenant(string propertyId, string unitId)
        {
            var filter = Builders<Property>.Filter.And(
                Builders<Property>.Filter.Eq(p => p.Id, propertyId),
                Builders<Property>.Filter.ElemMatch(p => p.Units, u => u.Id == unitId)
            );

            var update = Builders<Property>.Update
                .Set(p => p.Units[-1].TenantId, null)
                .Set(p => p.Units[-1].IsOccupied, false);

            var result = await _properties.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }
    }
}
