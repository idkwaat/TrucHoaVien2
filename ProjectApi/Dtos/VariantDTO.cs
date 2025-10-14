namespace ProjectApi.Dtos
{
    public class VariantDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? ModelUrl { get; set; }
    }
}
