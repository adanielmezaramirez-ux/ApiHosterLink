using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación
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
            if (!IsValidObjectId(propertyId))
                return BadRequest("ID de propiedad no válido");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Verificar permisos para ver la propiedad
            var propertyFilter = Builders<Property>.Filter.Eq(p => p.Id, propertyId) &
                               Builders<Property>.Filter.Eq(p => p.IsActive, true);
            var property = await _properties.Find(propertyFilter)
                .Project(p => new { p.Units, p.AdminId })
                .FirstOrDefaultAsync();

            if (property == null) return NotFound();

            // Control de acceso
            if (userRole == "Tenant")
            {
                // Tenant solo puede ver unidades donde es inquilino
                var tenantUnits = property.Units.Where(u => u.TenantId == userId).ToList();
                return Ok(tenantUnits);
            }
            else if (userRole == "Owner")
            {
                // Owner solo puede ver unidades donde es admin de la propiedad o owner de la unidad
                if (property.AdminId != userId && !property.Units.Any(u => u.OwnerId == userId))
                    return Forbid();

                return Ok(property.Units);
            }
            else if (userRole == "Admin")
            {
                // Admin puede ver todas las unidades
                return Ok(property.Units);
            }

            return Forbid();
        }

        [HttpPut("assign-tenant/{propertyId}/{unitId}")]
        [Authorize(Roles = "Admin,Owner")] // Solo admins y owners pueden asignar inquilinos
        public async Task<IActionResult> AssignTenant(string propertyId, string unitId, [FromBody] string tenantId)
        {
            if (!IsValidObjectId(propertyId) || !IsValidObjectId(unitId) || !IsValidObjectId(tenantId))
                return BadRequest("IDs no válidos");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Verificar que el usuario tiene permisos sobre la propiedad
            var propertyFilter = Builders<Property>.Filter.Eq(p => p.Id, propertyId) &
                               Builders<Property>.Filter.Eq(p => p.IsActive, true);
            var property = await _properties.Find(propertyFilter).FirstOrDefaultAsync();

            if (property == null) return NotFound("Propiedad no encontrada");

            // Verificar permisos
            if (!User.IsInRole("Admin") && property.AdminId != userId)
                return Forbid();

            // Verificar que la unidad existe
            var unitExists = property.Units.Any(u => u.Id == unitId);
            if (!unitExists) return NotFound("Unidad no encontrada");

            // Actualizar usando posición del array (más seguro que [-1])
            var filter = Builders<Property>.Filter.And(
                Builders<Property>.Filter.Eq(p => p.Id, propertyId),
                Builders<Property>.Filter.ElemMatch(p => p.Units, u => u.Id == unitId)
            );

            var update = Builders<Property>.Update
                .Set("Units.$.TenantId", tenantId)
                .Set("Units.$.IsOccupied", true);

            var result = await _properties.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpPut("remove-tenant/{propertyId}/{unitId}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> RemoveTenant(string propertyId, string unitId)
        {
            if (!IsValidObjectId(propertyId) || !IsValidObjectId(unitId))
                return BadRequest("IDs no válidos");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Verificar permisos
            var propertyFilter = Builders<Property>.Filter.Eq(p => p.Id, propertyId) &
                               Builders<Property>.Filter.Eq(p => p.IsActive, true);
            var property = await _properties.Find(propertyFilter).FirstOrDefaultAsync();

            if (property == null) return NotFound("Propiedad no encontrada");

            if (!User.IsInRole("Admin") && property.AdminId != userId)
                return Forbid();

            var filter = Builders<Property>.Filter.And(
                Builders<Property>.Filter.Eq(p => p.Id, propertyId),
                Builders<Property>.Filter.ElemMatch(p => p.Units, u => u.Id == unitId)
            );

            var update = Builders<Property>.Update
                .Set<object>("Units.$.TenantId", null)
                .Set("Units.$.IsOccupied", false);

            var result = await _properties.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        private bool IsValidObjectId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 24;
        }
    }
}