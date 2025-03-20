namespace PayMeChat_V_1.Models.Entities
{
    public class UsuarioLoginDto
    {
        public required string NombreUsuario { get; set; }
        public required string Contraseña { get; set; }
    }

    public class Usuario
    {
        public int Id { get; set; }
        public required string NombreUsuario { get; set; }
        public required string Correo { get; set; }
        public required string ContraseñaHash { get; set; }
        public required string ServidorURL { get; set; }
        public required string Telefono { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public required string TokenVerificacion { get; set; }
    }

    public class UsuarioToken
    {
        public int TokenID { get; set; }
        public int UsuarioID { get; set; }
        public required string Token { get; set; }
        public DateTime Expiracion { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
