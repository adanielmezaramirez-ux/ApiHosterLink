using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class SystemSettings
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("propertyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; }

    [BsonElement("paymentMethods")]
    public List<string> PaymentMethods { get; set; } = new List<string>();

    [BsonElement("maintenancePriorities")]
    public List<string> MaintenancePriorities { get; set; } = new List<string>();

    [BsonElement("notificationPreferences")]
    public Dictionary<string, bool> NotificationPreferences { get; set; } = new Dictionary<string, bool>();

    [BsonElement("currency")]
    public string Currency { get; set; } = "MXN";

    [BsonElement("timeZone")]
    public string TimeZone { get; set; } = "Central Standard Time";
}