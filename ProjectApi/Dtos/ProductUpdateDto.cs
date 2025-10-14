using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ProjectApi.DTOs
{
    public class ProductUpdateDto
    {

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string? DeletedVariantIds { get; set; }

        // Ảnh đại diện sản phẩm
        public IFormFile? DefaultImage { get; set; }
        public string? DefaultImageUrl { get; set; }

        // Danh sách biến thể
        public List<int>? VariantIds { get; set; }

        public List<string>? VariantNames { get; set; }
        public List<decimal>? VariantPrices { get; set; }
        public List<IFormFile>? VariantImages { get; set; }
        public List<IFormFile>? VariantModels { get; set; }


        public List<string>? VariantImageUrls { get; set; }
        public List<string>? VariantModelUrls { get; set; }
    }
}
