
namespace PayMeChat_V_1.Models.Entities
{
    public class ContactResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

