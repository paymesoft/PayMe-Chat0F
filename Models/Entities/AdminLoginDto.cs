namespace PayMeChat_V_1.Models.Entities
{
    public class AdminLoginDto
    {
        public required string Correo { get; set; }  // ✅ Se agrega la propiedad Correo
        public required string NombreUsuario { get; set; }
        public required string Contraseña { get; set; }
    }
}
