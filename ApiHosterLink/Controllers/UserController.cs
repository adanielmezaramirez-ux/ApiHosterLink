using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;

        public UserController(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _users.Find(u => u.IsActive).ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _users.Find(u => u.Id == id && u.IsActive).FirstOrDefaultAsync();
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            await _users.InsertOneAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        {
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
            return Ok(users);
        }
    }
}
