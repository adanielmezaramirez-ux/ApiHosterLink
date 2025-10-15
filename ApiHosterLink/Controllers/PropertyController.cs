using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyController : ControllerBase
    {
        private readonly IMongoCollection<Property> _properties;

        public PropertyController(IMongoDatabase database)
        {
            _properties = database.GetCollection<Property>("Properties");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Property>>> GetProperties()
        {
            var properties = await _properties.Find(p => p.IsActive).ToListAsync();
            return Ok(properties);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Property>> GetProperty(string id)
        {
            var property = await _properties.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();
            if (property == null) return NotFound();
            return Ok(property);
        }

        [HttpPost]
        public async Task<ActionResult<Property>> CreateProperty(Property property)
        {
            property.IsActive = true;
            await _properties.InsertOneAsync(property);
            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, property);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProperty(string id, Property updatedProperty)
        {
            var result = await _properties.ReplaceOneAsync(p => p.Id == id && p.IsActive, updatedProperty);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(string id)
        {
            var update = Builders<Property>.Update.Set(p => p.IsActive, false);
            var result = await _properties.UpdateOneAsync(p => p.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpGet("by-admin/{adminId}")]
        public async Task<ActionResult<IEnumerable<Property>>> GetPropertiesByAdmin(string adminId)
        {
            var properties = await _properties.Find(p => p.AdminId == adminId && p.IsActive).ToListAsync();
            return Ok(properties);
        }

        [HttpPost("{id}/add-unit")]
        public async Task<IActionResult> AddUnit(string id, Unit unit)
        {
            unit.Id = null; // MongoDB generará nuevo ID
            var update = Builders<Property>.Update.Push(p => p.Units, unit);
            var result = await _properties.UpdateOneAsync(p => p.Id == id && p.IsActive, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }
    }
}
