namespace PayMeChat_V_1.Models.Entities
{
    public class VerificarTokenDto
    {
        public required string Correo { get; set; }         // Correo del usuario
        public required string Token { get; set; }          // El token recibido por correo
    }
}


