namespace ProjectApi.Api.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? ModelUrl { get; set; }

        public string? CleanImageUrl { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // 🪶 Thông tin khắc
        public decimal? EngravingX { get; set; } // % theo chiều rộng ảnh
        public decimal? EngravingY { get; set; } // % theo chiều cao ảnh
        public string? EngravingColor { get; set; } = "#000000";
        public string? EngravingFont { get; set; } = "Arial";
        public string? EngravingText { get; set; }

        public int? EngravingSize { get; set; } = 22; // 📏 Cỡ chữ
        public decimal ExtraPrice { get; set; } = 0;
    }
}
