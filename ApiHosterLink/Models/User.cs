using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ApiHosterLink;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }  // Hacerlo nullable

    [BsonElement("name")]
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Name { get; set; }

    [BsonElement("email")]
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; }

    [BsonElement("passwordHash")]
    public string? PasswordHash { get; set; }  // Hacerlo nullable

    [BsonElement("phone")]
    public string? Phone { get; set; }  // Hacerlo nullable

    [BsonElement("role")]
    [Required(ErrorMessage = "El rol es requerido")]
    public string Role { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedad para recibir la contraseña en texto plano
    [BsonIgnore]
    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; }
}