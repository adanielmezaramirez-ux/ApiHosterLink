using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class MaintenanceRequest
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    [BsonElement("propertyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; }

    [BsonElement("unitId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UnitId { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; } // "Low", "Medium", "High", "Emergency"

    [BsonElement("status")]
    public string Status { get; set; } // "Pending", "InProgress", "Completed", "Cancelled"

    [BsonElement("assignedTo")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AssignedTo { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("images")]
    public List<string> Images { get; set; } = new List<string>();

    [BsonElement("estimatedCost")]
    public decimal? EstimatedCost { get; set; }

    [BsonElement("actualCost")]
    public decimal? ActualCost { get; set; }
    [BsonElement ("paiddate")]
    public DateTime PaidDate {  get; set; }
}