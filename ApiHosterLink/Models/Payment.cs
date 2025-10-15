using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class Payment
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    [BsonElement("propertyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; }

    [BsonElement("unitId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UnitId { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("paymentType")]
    public string PaymentType { get; set; } // "Rent", "Maintenance", "Service"

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } // "CreditCard", "DebitCard", "Cash", "Transfer"

    [BsonElement("status")]
    public string Status { get; set; } // "Pending", "Completed", "Failed", "Refunded"

    [BsonElement("dueDate")]
    public DateTime DueDate { get; set; }

    [BsonElement("paidDate")]
    public DateTime? PaidDate { get; set; }

    [BsonElement("transactionId")]
    public string TransactionId { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }
}