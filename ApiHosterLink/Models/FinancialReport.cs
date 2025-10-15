using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class FinancialReport
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("propertyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; }

    [BsonElement("period")]
    public string Period { get; set; } // "2024-01", "2024-Q1", "2024"

    [BsonElement("totalIncome")]
    public decimal TotalIncome { get; set; }

    [BsonElement("totalExpenses")]
    public decimal TotalExpenses { get; set; }

    [BsonElement("netBalance")]
    public decimal NetBalance { get; set; }

    [BsonElement("generatedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string GeneratedBy { get; set; }

    [BsonElement("generatedAt")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("details")]
    public List<ReportDetail> Details { get; set; } = new List<ReportDetail>();
}
