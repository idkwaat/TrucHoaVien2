using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ProjectApi.DTOs
{
    public class ProductCreateDto
    {
        // 🧱 Thông tin sản phẩm cha
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }

        // 🧩 Danh sách biến thể (nhiều loại quạt)
        public List<string>? VariantNames { get; set; }
        public List<decimal>? VariantPrices { get; set; }
        public List<IFormFile>? VariantImages { get; set; }
        public List<IFormFile>? VariantModels { get; set; }

        public List<IFormFile>? VariantCleanImages { get; set; }
        public List<string>? VariantCleanImageUrls { get; set; } // nếu không upload lại


        // 🔁 Dùng cho khi edit mà không upload lại file (nếu cần)
        public List<string>? VariantImageUrls { get; set; }
        public List<string>? VariantModelUrls { get; set; }
    }
}
