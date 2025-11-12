using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserController(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _users.Find(u => u.IsActive).ToListAsync();
            // No devolvemos el PasswordHash por seguridad
            users.ForEach(u => u.PasswordHash = null);
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _users.Find(u => u.Id == id && u.IsActive).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            // No devolvemos el PasswordHash por seguridad
            user.PasswordHash = null;
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            // Validar que se proporcionó una contraseña
            if (string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("La contraseña es requerida");
            }

            // Hashear la contraseña antes de guardar
            user.PasswordHash = _passwordHasher.HashPassword(user, user.Password);
            user.Password = null; // Limpiar la contraseña en texto plano

            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _users.InsertOneAsync(user);

            // No devolvemos el PasswordHash en la respuesta
            user.PasswordHash = null;
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        {
            // Si se está actualizando la contraseña, hashearla
            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                updatedUser.PasswordHash = _passwordHasher.HashPassword(updatedUser, updatedUser.Password);
                updatedUser.Password = null;
            }
            else
            {
                // Si no se actualiza la contraseña, mantener la actual
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
            var update = Builders<User>.Update.Set(u => u.IsActive, false);
            var result = await _users.UpdateOneAsync(u => u.Id == id, update);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpGet("by-role/{role}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersByRole(string role)
        {
            var users = await _users.Find(u => u.Role == role && u.IsActive).ToListAsync();
            // No devolvemos el PasswordHash por seguridad
            users.ForEach(u => u.PasswordHash = null);
            return Ok(users);
        }
    }
}