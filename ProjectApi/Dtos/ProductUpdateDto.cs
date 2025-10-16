using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ProjectApi.DTOs
{
    public class ProductUpdateDto
    {
        // 🧱 Thông tin sản phẩm cha
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Price { get; set; } // ✅ Giữ nullable vì không phải product cha nào cũng có giá
        public int CategoryId { get; set; }

        // 🗑️ Danh sách ID biến thể bị xóa (gửi từ client dạng JSON string)
        public string? DeletedVariantIds { get; set; }

        // 🖼️ Ảnh đại diện sản phẩm cha
        public IFormFile? DefaultImage { get; set; }
        public string? DefaultImageUrl { get; set; }

        // 🧩 Danh sách biến thể
        public List<int>? VariantIds { get; set; }
        public List<string>? VariantNames { get; set; }
        public List<decimal>? VariantPrices { get; set; }

        // 🖼️ File upload (form-data)
        public List<IFormFile>? VariantImages { get; set; }
        public List<IFormFile>? VariantModels { get; set; }

        // 🌐 Link cũ (khi không upload lại)
        public List<string>? VariantImageUrls { get; set; }
        public List<string>? VariantModelUrls { get; set; }
    }
}
