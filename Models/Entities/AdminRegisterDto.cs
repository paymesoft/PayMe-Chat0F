namespace PayMeChat_V_1.Models.Entities
{
    public class AdminRegisterDto
    {
        public required string NombreUsuario { get; set; }
        public required string Contraseña { get; set; }
        public required string Correo { get; set; }
    }
}
