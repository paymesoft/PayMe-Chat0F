namespace PayMeChat_V_1.Models.Entities
{
    public class ClientesAdmin
    {
        public int ID { get; set; }  // Ya no es 'required'
        public required string NombreCliente { get; set; }
        public required string NombreRepre { get; set; }
        public required string Correo { get; set; }
        public required string Telefono { get; set; }
        public required string DireccionEmpresa { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow; // Asigna automáticamente
        public bool Activo { get; set; } = true;  // Siempre activo por defecto
    }
}
