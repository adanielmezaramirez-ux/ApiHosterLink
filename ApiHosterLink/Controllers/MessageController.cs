using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiHosterLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todos los mensajes requieren autenticación
    public class MessageController : ControllerBase
    {
        private readonly IMongoCollection<Message> _messages;

        public MessageController(IMongoDatabase database)
        {
            _messages = database.GetCollection<Message>("Messages");
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<object>>> GetConversations()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Obtener conversaciones únicas (agrupadas por propertyId y receiverId/senderId)
            var sentMessages = await _messages.Find(m => m.SenderId == userId)
                .SortByDescending(m => m.SentAt)
                .ToListAsync();

            var receivedMessages = await _messages.Find(m => m.ReceiverId == userId)
                .SortByDescending(m => m.SentAt)
                .ToListAsync();

            // Combinar y agrupar conversaciones
            var allMessages = sentMessages.Concat(receivedMessages);
            var conversations = allMessages
                .GroupBy(m => m.PropertyId + "_" +
                    (m.SenderId == userId ? m.ReceiverId : m.SenderId))
                .Select(g => new
                {
                    PropertyId = g.First().PropertyId,
                    OtherUserId = g.First().SenderId == userId ? g.First().ReceiverId : g.First().SenderId,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                })
                .ToList();

            return Ok(conversations);
        }

        [HttpGet("conversation/{propertyId}/{otherUserId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetConversation(string propertyId, string otherUserId)
        {
            if (!IsValidObjectId(propertyId) || !IsValidObjectId(otherUserId))
                return BadRequest("IDs no válidos");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var filter = Builders<Message>.Filter.And(
                Builders<Message>.Filter.Eq(m => m.PropertyId, propertyId),
                Builders<Message>.Filter.Or(
                    Builders<Message>.Filter.And(
                        Builders<Message>.Filter.Eq(m => m.SenderId, userId),
                        Builders<Message>.Filter.Eq(m => m.ReceiverId, otherUserId)
                    ),
                    Builders<Message>.Filter.And(
                        Builders<Message>.Filter.Eq(m => m.SenderId, otherUserId),
                        Builders<Message>.Filter.Eq(m => m.ReceiverId, userId)
                    )
                )
            );

            var messages = await _messages.Find(filter)
                .SortBy(m => m.SentAt)
                .Limit(100) // Limitar historial de mensajes
                .ToListAsync();

            // Marcar mensajes como leídos
            var unreadFilter = Builders<Message>.Filter.And(
                filter,
                Builders<Message>.Filter.Eq(m => m.ReceiverId, userId),
                Builders<Message>.Filter.Eq(m => m.IsRead, false)
            );

            if (await _messages.CountDocumentsAsync(unreadFilter) > 0)
            {
                var update = Builders<Message>.Update.Set(m => m.IsRead, true);
                await _messages.UpdateManyAsync(unreadFilter, update);
            }

            return Ok(messages);
        }

        [HttpPost]
        public async Task<ActionResult<Message>> SendMessage(Message message)
        {
            // Validaciones
            if (string.IsNullOrEmpty(message.ReceiverId))
                return BadRequest("Receptor es requerido");

            if (!IsValidObjectId(message.ReceiverId))
                return BadRequest("ID de receptor no válido");

            if (string.IsNullOrEmpty(message.PropertyId))
                return BadRequest("PropertyId es requerido");

            if (!IsValidObjectId(message.PropertyId))
                return BadRequest("ID de propiedad no válido");

            if (string.IsNullOrEmpty(message.Content) || message.Content.Length < 1)
                return BadRequest("El contenido del mensaje es requerido");

            if (message.Content.Length > 1000)
                return BadRequest("El mensaje es demasiado largo (máximo 1000 caracteres)");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Asignar remitente y fecha
            message.SenderId = userId;
            message.SentAt = DateTime.UtcNow;
            message.IsRead = false;

            await _messages.InsertOneAsync(message);

            // Aquí podrías agregar notificación push/webhook
            return CreatedAtAction(nameof(GetConversation),
                new { propertyId = message.PropertyId, otherUserId = message.ReceiverId },
                message);
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkMessageAsRead(string id)
        {
            if (!IsValidObjectId(id))
                return BadRequest("ID de mensaje no válido");

            var message = await _messages.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (message == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Solo el receptor puede marcar como leído
            if (message.ReceiverId != userId)
                return Forbid();

            var result = await _messages.UpdateOneAsync(
                m => m.Id == id,
                Builders<Message>.Update.Set(m => m.IsRead, true)
            );

            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<object>> GetUnreadCount()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var filter = Builders<Message>.Filter.Eq(m => m.ReceiverId, userId) &
                         Builders<Message>.Filter.Eq(m => m.IsRead, false);

            var unreadCount = await _messages.CountDocumentsAsync(filter);

            return Ok(new { UnreadCount = unreadCount });
        }

        private bool IsValidObjectId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 24;
        }
    }
}