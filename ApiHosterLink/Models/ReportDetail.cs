using MongoDB.Bson.Serialization.Attributes;

namespace ApiHosterLink;

public class ReportDetail
{
    [BsonElement("category")]
    public string Category { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }
}