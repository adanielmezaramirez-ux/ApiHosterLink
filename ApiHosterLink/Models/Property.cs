using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class Property
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("address")]
    public string Address { get; set; }

    [BsonElement("adminId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AdminId { get; set; }

    [BsonElement("units")]
    public List<Unit> Units { get; set; } = new List<Unit>();

    [BsonElement("amenities")]
    public List<string> Amenities { get; set; } = new List<string>();

    [BsonElement("monthlyFee")]
    public decimal MonthlyFee { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}