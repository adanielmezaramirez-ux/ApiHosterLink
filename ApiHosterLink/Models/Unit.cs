using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class Unit
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("unitNumber")]
    public string UnitNumber { get; set; }

    [BsonElement("propertyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; }

    [BsonElement("tenantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TenantId { get; set; }

    [BsonElement("ownerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OwnerId { get; set; }

    [BsonElement("rentAmount")]
    public decimal RentAmount { get; set; }

    [BsonElement("maintenanceFee")]
    public decimal MaintenanceFee { get; set; }

    [BsonElement("isOccupied")]
    public bool IsOccupied { get; set; }

    [BsonElement("features")]
    public List<string> Features { get; set; } = new List<string>();
}