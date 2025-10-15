using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class Document
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } // "Contract", "Invoice", "Report", "Other"

    [BsonElement("fileName")]
    public string FileName { get; set; }

    [BsonElement("fileUrl")]
    public string FileUrl { get; set; }

    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    [BsonElement("propertyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; }

    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isPublic")]
    public bool IsPublic { get; set; } = false;
}
