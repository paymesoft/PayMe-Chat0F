
namespace PayMeChat.V1.Backend.Entities;

using System.Text.Json.Serialization;

public class UsuarioPayMeChat
{
    public int Id { get; set; }

    public required string NombreUsuario { get; set; }
    public required string ContraseñaHash { get; set; }
    public required string ServidorURL { get; set; }
    public required string Telefono { get; set; }
    public required string Correo { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

