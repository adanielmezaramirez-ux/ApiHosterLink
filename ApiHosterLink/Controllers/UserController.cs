using System.Collections.Generic;
using System.Threading.Tasks;
using ApiHosterLink.Helpers;
using ApiHosterLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Solo administradores pueden gestionar usuarios
    public class UserController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IValidationService _validationService;
        public UserController(IMongoDatabase database, IValidationService validationService)
        {
            _users = database.GetCollection<User>("Users");
            _passwordHasher = new PasswordHasher<User>();
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetUsers(int page = 1, int pageSize = 20)
        {
            var filter = Builders<User>.Filter.Eq(u => u.IsActive, true);

            // Usar paginación optimizada
            var (results, total) = await MongoDBHelper.GetPaginatedResults(
                _users, filter, Builders<User>.Sort.Descending(u => u.CreatedAt), page, pageSize);

            // Excluir información sensible
            results.ForEach(u => {
                u.PasswordHash = null;
                u.Password = null;
            });

            return Ok(new
            {
                Users = results,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            if (!_validationService.IsValidObjectId(id))
                return BadRequest("ID de usuario no válido");

            // Usar proyección para solo campos necesarios
            var projection = Builders<User>.Projection
                .Exclude(u => u.PasswordHash)
                .Exclude(u => u.Password);

            var user = await _users.Find(u => u.Id == id && u.IsActive)
                .Project<User>(projection)
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        [AllowAnonymous] // Permitir registro sin autenticación
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            // Validaciones adicionales
            if (string.IsNullOrEmpty(user.Password) || user.Password.Length < 6)
                return BadRequest("La contraseña debe tener al menos 6 caracteres");

            if (string.IsNullOrEmpty(user.Email))
                return BadRequest("El email es requerido");

            // Verificar si el email ya existe
            var existingUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest("El email ya está registrado");

            // Hashear la contraseña antes de guardar
            user.PasswordHash = _passwordHasher.HashPassword(user, user.Password);
            user.Password = null;

            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _users.InsertOneAsync(user);
            user.PasswordHash = null;
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de usuario no válido");

            // Si se está actualizando la contraseña, validar y hashear
            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                if (updatedUser.Password.Length < 6)
                    return BadRequest("La contraseña debe tener al menos 6 caracteres");

                updatedUser.PasswordHash = _passwordHasher.HashPassword(updatedUser, updatedUser.Password);
                updatedUser.Password = null;
            }
            else
            {
                var existingUser = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    updatedUser.PasswordHash = existingUser.PasswordHash;
                }
            }

            var result = await _users.ReplaceOneAsync(u => u.Id == id && u.IsActive, updatedUser);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de usuario no válido");

            var update = Builders<User>.Update.Set(u => u.IsActive, false);
            var result = await _users.UpdateOneAsync(u => u.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpGet("by-role/{role}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersByRole(string role)
        {
            // Validar rol válido
            var validRoles = new[] { "Admin", "Tenant", "Owner" };
            if (!validRoles.Contains(role))
                return BadRequest("Rol no válido");

            var users = await _users.Find(u => u.Role == role && u.IsActive).ToListAsync();
            users.ForEach(u => u.PasswordHash = null);
            return Ok(users);
        }

        // Método helper para validar ObjectId
        private bool IsValidObjectId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 24;
        }
    }
}