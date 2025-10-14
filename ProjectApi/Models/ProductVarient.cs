namespace ProjectApi.Api.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? ModelUrl { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
