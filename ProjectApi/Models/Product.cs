using ProjectApi.Models;

namespace ProjectApi.Api.Models
{
    public class Product
    {
        public int Id { get; set; }

        // 🔹 Tên cha (label sản phẩm)
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // 🔹 Liên kết danh mục
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // 🔹 Ảnh mặc định (nếu bạn muốn hiển thị preview)
        public string? ImageUrl { get; set; }

        // 🔹 Danh sách biến thể (đây mới là sản phẩm thực tế)
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        public ICollection<Review>? Reviews { get; set; } = new List<Review>();
    }
}
