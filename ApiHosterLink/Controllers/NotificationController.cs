using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var notifications = await _notifications.Find(n => n.UserId == userId)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(notifications);
        }

        [HttpGet("unread/{userId}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadNotifications(string userId)
        {
            var notifications = await _notifications.Find(n => n.UserId == userId && !n.IsRead)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(notifications);
        }

        [HttpPost]
        public async Task<ActionResult<Notification>> CreateNotification(Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;
            await _notifications.InsertOneAsync(notification);
            return CreatedAtAction(nameof(GetNotificationsByUser), new { userId = notification.UserId }, notification);
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
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
            var result = await _notifications.UpdateManyAsync(
                n => n.UserId == userId && !n.IsRead,
                Builders<Notification>.Update.Set(n => n.IsRead, true)
            );
            return NoContent();
        }
    }
}
