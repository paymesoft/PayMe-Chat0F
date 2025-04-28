namespace PayMeChat_V_1.Models.Entities
{
    public class TemplateResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
