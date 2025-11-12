using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ApiHosterLink.Services;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            // Buscar usuario por email
            var user = await _users.Find(u => u.Email == loginRequest.Email && u.IsActive).FirstOrDefaultAsync();

            if (user == null)
            {
                return Unauthorized("Credenciales inválidas");
            }

            // Verificar contraseña
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginRequest.Password);

            if (result != PasswordVerificationResult.Success)
            {
                return Unauthorized("Credenciales inválidas");
            }

            // Generar token
            var token = _jwtService.GenerateToken(user);

            var response = new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Deberías obtener esto de la configuración
            };

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register(User user)
        {
            // Verificar si el email ya existe
            var existingUser = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return BadRequest("El email ya está registrado");
            }

            // Validar que se proporcionó una contraseña
            if (string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("La contraseña es requerida");
            }

            // Hashear la contraseña
            user.PasswordHash = _passwordHasher.HashPassword(user, user.Password);
            user.Password = null;

            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _users.InsertOneAsync(user);

            // Generar token automáticamente después del registro
            var token = _jwtService.GenerateToken(user);

            var response = new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };

            return CreatedAtAction(nameof(Login), response);
        }
    }
}