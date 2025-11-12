using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todas las notificaciones requieren autenticación
    public class NotificationController : ControllerBase
    {
        private readonly IMongoCollection<Notification> _notifications;

        public NotificationController(IMongoDatabase database)
        {
            _notifications = database.GetCollection<Notification>("Notifications");
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotificationsByUser(string userId)
        {
            if (!IsValidObjectId(userId))
                return BadRequest("ID de usuario no válido");

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Solo puede ver sus propias notificaciones o si es Admin
            if (userRole != "Admin" && userId != currentUserId)
                return Forbid();

            var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
            var notifications = await _notifications.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Limit(50) // Limitar para no sobrecargar
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpGet("unread/{userId}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadNotifications(string userId)
        {
            if (!IsValidObjectId(userId))
                return BadRequest("ID de usuario no válido");

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userRole != "Admin" && userId != currentUserId)
                return Forbid();

            var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId) &
                         Builders<Notification>.Filter.Eq(n => n.IsRead, false);

            var notifications = await _notifications.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Limit(20) // Limitar notificaciones no leídas
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Solo el sistema (admin) puede crear notificaciones
        public async Task<ActionResult<Notification>> CreateNotification(Notification notification)
        {
            // Validaciones
            if (string.IsNullOrEmpty(notification.UserId))
                return BadRequest("UserId es requerido");

            if (!IsValidObjectId(notification.UserId))
                return BadRequest("ID de usuario no válido");

            if (string.IsNullOrEmpty(notification.Title) || notification.Title.Length < 2)
                return BadRequest("El título debe tener al menos 2 caracteres");

            if (string.IsNullOrEmpty(notification.Message) || notification.Message.Length < 5)
                return BadRequest("El mensaje debe tener al menos 5 caracteres");

            // Validar tipo de notificación
            var validTypes = new[] { "Payment", "Maintenance", "System", "Alert" };
            if (!validTypes.Contains(notification.Type))
                return BadRequest("Tipo de notificación no válido");

            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;

            // Si hay entidad relacionada, validar el ID
            if (!string.IsNullOrEmpty(notification.RelatedEntityId) && !IsValidObjectId(notification.RelatedEntityId))
                return BadRequest("ID de entidad relacionada no válido");

            await _notifications.InsertOneAsync(notification);

            return CreatedAtAction(nameof(GetNotificationsByUser),
                new { userId = notification.UserId }, notification);
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de notificación no válido");

            var notification = await _notifications.Find(n => n.Id == id).FirstOrDefaultAsync();
            if (notification == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Solo el dueño de la notificación o un Admin puede marcarla como leída
            if (notification.UserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _notifications.UpdateOneAsync(
                n => n.Id == id,
                Builders<Notification>.Update.Set(n => n.IsRead, true)
            );

            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpPut("mark-all-read/{userId}")]
        public async Task<IActionResult> MarkAllAsRead(string userId)
        {
            if (!IsValidObjectId(userId))
                return BadRequest("ID de usuario no válido");

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Solo puede marcar sus propias notificaciones como leídas
            if (userRole != "Admin" && userId != currentUserId)
                return Forbid();

            var result = await _notifications.UpdateManyAsync(
                n => n.UserId == userId && !n.IsRead,
                Builders<Notification>.Update.Set(n => n.IsRead, true)
            );

            return Ok(new
            {
                Message = "Todas las notificaciones marcadas como leídas",
                UpdatedCount = result.ModifiedCount
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(string id)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de notificación no válido");

            var notification = await _notifications.Find(n => n.Id == id).FirstOrDefaultAsync();
            if (notification == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Solo el dueño de la notificación o un Admin puede eliminarla
            if (notification.UserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _notifications.DeleteOneAsync(n => n.Id == id);
            if (result.DeletedCount == 0) return NotFound();

            return NoContent();
        }

        private bool IsValidObjectId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 24;
        }
    }
}