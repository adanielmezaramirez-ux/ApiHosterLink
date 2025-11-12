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
    [Authorize] // Todos los endpoints requieren autenticación
    public class PropertyController : ControllerBase
    {
        private readonly IMongoCollection<Property> _properties;
        private readonly IValidationService _validationService;

        public PropertyController(IMongoDatabase database, IValidationService validationService)
        {
            _properties = database.GetCollection<Property>("Properties");
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetProperties(int page = 1, int pageSize = 20)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            FilterDefinition<Property> filter;
            if (userRole == "Admin")
            {
                filter = Builders<Property>.Filter.Eq(p => p.IsActive, true);
            }
            else if (userRole == "Owner")
            {
                filter = Builders<Property>.Filter.Eq(p => p.AdminId, userId) &
                         Builders<Property>.Filter.Eq(p => p.IsActive, true);
            }
            else
            {
                return Forbid();
            }

            var (results, total) = await MongoDBHelper.GetPaginatedResults(
                _properties, filter, Builders<Property>.Sort.Ascending(p => p.Name), page, pageSize);

            return Ok(new
            {
                Properties = results,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Property>> GetProperty(string id)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de propiedad no válido");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var property = await _properties.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();
            if (property == null) return NotFound();

            // Verificar permisos
            if (userRole != "Admin" && property.AdminId != userId)
                return Forbid();

            return Ok(property);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Owner")] // Solo admins y owners pueden crear propiedades
        public async Task<ActionResult<Property>> CreateProperty(Property property)
        {
            // Validaciones
            if (string.IsNullOrEmpty(property.Name) || property.Name.Length < 2)
                return BadRequest("El nombre de la propiedad debe tener al menos 2 caracteres");

            if (string.IsNullOrEmpty(property.Address))
                return BadRequest("La dirección es requerida");

            if (property.MonthlyFee < 0)
                return BadRequest("La cuota mensual no puede ser negativa");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Si es Owner, se auto-asigna como admin
            if (User.IsInRole("Owner"))
            {
                property.AdminId = userId;
            }

            property.IsActive = true;
            await _properties.InsertOneAsync(property);
            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, property);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProperty(string id, Property updatedProperty)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de propiedad no válido");

            // Verificar permisos primero
            var existingProperty = await _properties.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();
            if (existingProperty == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (User.IsInRole("Admin") || existingProperty.AdminId == userId)
            {
                // Solo el admin de la propiedad o un Admin global pueden actualizar
                var result = await _properties.ReplaceOneAsync(p => p.Id == id && p.IsActive, updatedProperty);
                if (result.MatchedCount == 0) return NotFound();
                return NoContent();
            }

            return Forbid();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Solo admins pueden eliminar propiedades
        public async Task<IActionResult> DeleteProperty(string id)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de propiedad no válido");

            var update = Builders<Property>.Update.Set(p => p.IsActive, false);
            var result = await _properties.UpdateOneAsync(p => p.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpGet("by-admin/{adminId}")]
        public async Task<ActionResult<IEnumerable<Property>>> GetPropertiesByAdmin(string adminId)
        {
            if (!IsValidObjectId(adminId))
                return BadRequest("ID de administrador no válido");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Solo puede ver sus propias propiedades o si es Admin
            if (userRole != "Admin" && adminId != userId)
                return Forbid();

            var filter = Builders<Property>.Filter.Eq(p => p.AdminId, adminId) &
                         Builders<Property>.Filter.Eq(p => p.IsActive, true);
            var properties = await _properties.Find(filter).ToListAsync();
            return Ok(properties);
        }

        [HttpPost("{id}/add-unit")]
        public async Task<IActionResult> AddUnit(string id, Unit unit)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de propiedad no válido");

            // Validaciones de unidad
            if (string.IsNullOrEmpty(unit.UnitNumber))
                return BadRequest("El número de unidad es requerido");

            if (unit.RentAmount < 0 || unit.MaintenanceFee < 0)
                return BadRequest("Los montos no pueden ser negativos");

            // Verificar permisos
            var property = await _properties.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();
            if (property == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (User.IsInRole("Admin") || property.AdminId == userId)
            {
                unit.Id = null; // MongoDB generará nuevo ID
                var update = Builders<Property>.Update.Push(p => p.Units, unit);
                var result = await _properties.UpdateOneAsync(p => p.Id == id && p.IsActive, update);
                if (result.MatchedCount == 0) return NotFound();
                return NoContent();
            }

            return Forbid();
        }

        private bool IsValidObjectId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 24;
        }
    }
}