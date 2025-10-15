using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class Message
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("senderId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SenderId { get; set; }

    [BsonElement("receiverId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReceiverId { get; set; }

    [BsonElement("propertyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; }

    [BsonElement("content")]
    public string Content { get; set; }

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [BsonElement("attachments")]
    public List<string> Attachments { get; set; } = new List<string>();
}