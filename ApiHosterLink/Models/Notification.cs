using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class Notification
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("message")]
    public string Message { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } // "Payment", "Maintenance", "System", "Alert"

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("relatedEntity")]
    public string RelatedEntity { get; set; } // "Payment", "MaintenanceRequest", etc.

    [BsonElement("relatedEntityId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RelatedEntityId { get; set; }
}