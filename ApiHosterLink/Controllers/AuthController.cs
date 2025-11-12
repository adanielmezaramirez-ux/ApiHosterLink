using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ApiHosterLink.Services;
using Microsoft.AspNetCore.Authorization;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Este controlador es público
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtService _jwtService;

        public AuthController(IMongoDatabase database, IJwtService jwtService)
        {
            _users = database.GetCollection<User>("Users");
            _passwordHasher = new PasswordHasher<User>();
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest)
        {
            // Validaciones adicionales
            if (string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                return BadRequest("Email y contraseña son requeridos");

            // Buscar usuario por email - sanitizado
            var filter = Builders<User>.Filter.Eq(u => u.Email, loginRequest.Email.Trim().ToLower());
            var user = await _users.Find(filter & Builders<User>.Filter.Eq(u => u.IsActive, true)).FirstOrDefaultAsync();

            if (user == null)
            {
                // No revelar si el usuario existe o no por seguridad
                return Unauthorized("Credenciales inválidas");
            }

            // Verificar contraseña
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginRequest.Password);

            if (result != PasswordVerificationResult.Success)
                return Unauthorized("Credenciales inválidas");

            // Generar token
            var token = _jwtService.GenerateToken(user);

            var response = new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(180) // Coincide con JWT service
            };

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register(User user)
        {
            // Validaciones robustas
            if (string.IsNullOrEmpty(user.Name) || user.Name.Length < 2)
                return BadRequest("El nombre debe tener al menos 2 caracteres");

            if (string.IsNullOrEmpty(user.Email))
                return BadRequest("El email es requerido");

            if (string.IsNullOrEmpty(user.Password) || user.Password.Length < 6)
                return BadRequest("La contraseña debe tener al menos 6 caracteres");

            if (string.IsNullOrEmpty(user.Role))
                return BadRequest("El rol es requerido");

            // Validar rol
            var validRoles = new[] { "Admin", "Tenant", "Owner" };
            if (!validRoles.Contains(user.Role))
                return BadRequest("Rol no válido");

            // Verificar si el email ya existe - sanitizado
            var emailFilter = Builders<User>.Filter.Eq(u => u.Email, user.Email.Trim().ToLower());
            var existingUser = await _users.Find(emailFilter).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest("El email ya está registrado");

            // Crear nuevo usuario
            user.Email = user.Email.Trim().ToLower();
            user.PasswordHash = _passwordHasher.HashPassword(user, user.Password);
            user.Password = null;
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _users.InsertOneAsync(user);

            // Generar token automáticamente
            var token = _jwtService.GenerateToken(user);

            var response = new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(180)
            };

            return CreatedAtAction(nameof(Login), response);
        }

        [HttpPost("logout")]
        [Authorize] // Requiere autenticación pero cualquier rol
        public IActionResult Logout()
        {
            // En JWT stateless, el logout es manejado en el cliente
            // Pero podemos invalidar tokens si implementamos blacklist
            return Ok(new { message = "Sesión cerrada exitosamente" });
        }
    }
}