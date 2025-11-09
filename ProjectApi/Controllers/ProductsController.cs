using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Api.Models;
using ProjectApi.Data;
using ProjectApi.DTOs;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ProjectApi.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly FurnitureDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly Cloudinary _cloudinary;

        public ProductsController(FurnitureDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;

            // ✅ Cấu hình Cloudinary (thay bằng key của bạn)
            var account = new Account(
                "drequjrqq",
                "388973676734171",
                "QQWKtTgzykIRTB0xRtQJ054pjF8"
            );
            _cloudinary = new Cloudinary(account);
        }

        // 🟢 Lấy tất cả sản phẩm (cha + toàn bộ biến thể, có tìm kiếm & phân trang)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] int? categoryId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
            if (page < 1) page = 1;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .AsQueryable();

            // 🔍 Tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(keyword) ||
                    (p.Description != null && p.Description.ToLower().Contains(keyword)));
            }

            // 🏷️ Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // 💰 Lọc theo giá — vẫn giữ sản phẩm nếu chưa có biến thể
            if (minPrice.HasValue)
            {
                query = query.Where(p =>
                    !p.Variants.Any() || p.Variants.Any(v => v.Price >= minPrice.Value));
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p =>
                    !p.Variants.Any() || p.Variants.Any(v => v.Price <= maxPrice.Value));
            }

            // 🧮 Tổng sản phẩm
            var totalItems = await query.CountAsync();

            // 📄 Phân trang
            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ✨ Map sang DTO
            var result = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.CategoryId,
                CategoryName = p.Category?.Name ?? "Không có danh mục",
                AverageRating = p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = p.Reviews.Count,
                Variants = (p.Variants != null && p.Variants.Any())
    ? p.Variants.Select(v => new
    {
        v.Id,
        v.Name,
        v.Price,
        v.ImageUrl,
        v.CleanImageUrl, // ✅ thêm dòng này
        v.ModelUrl
    }).Cast<object>().ToList()
    : new List<object>(),

                // Không có biến thể vẫn trả rỗng
                MinPrice = p.Variants.Any() ? p.Variants.Min(v => v.Price) : 0,
                MaxPrice = p.Variants.Any() ? p.Variants.Max(v => v.Price) : 0
            });

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Data = result
            });
        }





        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            var averageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0;
            var reviewCount = product.Reviews.Count;

            return Ok(new
            {
                product.Id,
                product.Name,
                product.Description,
                product.ImageUrl,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? "Không có danh mục",
                AverageRating = Math.Round(averageRating, 1), // ⭐ trung bình sao
                ReviewCount = reviewCount,
                Variants = product.Variants.Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.Price,
                    v.ImageUrl,
                    v.CleanImageUrl, // ✅ thêm dòng này
                    v.ModelUrl
                })

            });
        }


        // 🟢 Top sản phẩm tháng (30 ngày gần nhất)
        [HttpGet("top-month")]
        public async Task<IActionResult> GetTopProductsOfMonth()
        {
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            // Lấy toàn bộ sản phẩm
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .ToListAsync();

            // Xử lý tính điểm trung bình trong tháng (nếu có)
            var result = products.Select(p =>
            {
                var recentReviews = p.Reviews
                    .Where(r => r.CreatedAt >= oneMonthAgo)
                    .ToList();

                double avgRating = 0;
                if (recentReviews.Any())
                {
                    avgRating = Math.Round(recentReviews.Average(r => r.Rating), 1);
                }
                else if (p.Reviews.Any())
                {
                    // Nếu không có review trong tháng thì lấy điểm trung bình toàn bộ
                    avgRating = Math.Round(p.Reviews.Average(r => r.Rating), 1);
                }

                return new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "Không có danh mục",
                    AverageRating = avgRating,
                    ReviewCount = p.Reviews.Count,
                    ImageUrl = p.Variants.FirstOrDefault()?.ImageUrl,
                    Variants = p.Variants.Select(v => new
                    {
                        v.Id,
                        v.Name,
                        v.Price,
                        v.ImageUrl
                    }).ToList()
                };
            })
            // Ưu tiên: có review trong tháng > có review cũ > chưa có review
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.ReviewCount)
            .Take(5)
            .ToList();

            return Ok(new { Data = result });
        }



        [HttpPost("create")]
        [RequestSizeLimit(200_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ProductCreateDto dto)
        {
            Console.WriteLine("===== FORM DEBUG =====");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"{key}: {string.Join(", ", Request.Form[key])}");
            }
            Console.WriteLine($"🧾 Tổng file: {Request.Form.Files.Count}");
            foreach (var file in Request.Form.Files)
            {
                Console.WriteLine($"📷 {file.Name} - {file.FileName}");
            }
            Console.WriteLine("=======================");

            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên sản phẩm không được để trống.");

            string uploadPath = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads"
            );
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // 🔹 Tạo sản phẩm cha
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                CategoryId = dto.CategoryId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 🔹 Thêm biến thể
            if (dto.VariantNames != null && dto.VariantNames.Any())
            {
                for (int i = 0; i < dto.VariantNames.Count; i++)
                {
                    string name = dto.VariantNames[i];
                    decimal price = dto.VariantPrices?.ElementAtOrDefault(i) ?? 0;

                    // ✅ Lấy đúng file từ form
                    var imageFile = Request.Form.Files.FirstOrDefault(f =>
                        f.Name == $"VariantImages[{i}]" || (i == 0 && f.Name == "VariantImages")
                    );

                    var cleanImageFile = Request.Form.Files.FirstOrDefault(f =>
                        f.Name == $"VariantCleanImages[{i}]" || (i == 0 && f.Name == "VariantCleanImages")
                    );

                    var modelFile = Request.Form.Files.FirstOrDefault(f =>
                        f.Name == $"VariantModels[{i}]" || (i == 0 && f.Name == "VariantModels")
                    );

                    string? imageUrl = null, cleanImageUrl = null, modelUrl = null;

                    // 🖼️ Upload ảnh chính
                    if (imageFile != null)
                    {
                        try
                        {
                            using var stream = imageFile.OpenReadStream();
                            var uploadParams = new ImageUploadParams
                            {
                                File = new FileDescription(imageFile.FileName, stream),
                                Folder = "uploads/images"
                            };
                            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                            imageUrl = uploadResult.SecureUrl.ToString();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Lỗi upload ảnh: {ex.Message}");
                        }
                    }

                    // 🖼️ Upload ảnh trơn (Clean)
                    if (cleanImageFile != null)
                    {
                        try
                        {
                            using var stream = cleanImageFile.OpenReadStream();
                            var uploadParams = new ImageUploadParams
                            {
                                File = new FileDescription(cleanImageFile.FileName, stream),
                                Folder = "uploads/clean-images"
                            };
                            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                            cleanImageUrl = uploadResult.SecureUrl.ToString();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Lỗi upload ảnh trơn: {ex.Message}");
                        }
                    }

                    // 🧱 Upload model (3D)
                    if (modelFile != null)
                    {
                        try
                        {
                            Console.WriteLine($"⬆️ Uploading model: {modelFile.FileName} ({modelFile.Length / 1024 / 1024} MB)");

                            using var stream = modelFile.OpenReadStream();

                            var uploadParams = new RawUploadParams
                            {
                                File = new FileDescription(modelFile.FileName, stream),
                                Folder = "uploads/models",
                                UseFilename = true,
                                UniqueFilename = false
                            };

                            RawUploadResult uploadResult;

                            if (modelFile.Length > 20 * 1024 * 1024)
                                uploadResult = await _cloudinary.UploadLargeAsync(uploadParams);
                            else
                                uploadResult = await _cloudinary.UploadAsync(uploadParams);

                            modelUrl = uploadResult.SecureUrl?.ToString();
                            Console.WriteLine($"✅ Uploaded model: {modelUrl}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Lỗi upload model: {ex.Message}");
                        }
                    }

                    // 🔹 Lưu biến thể
                    _context.ProductVariants.Add(new ProductVariant
                    {
                        ProductId = product.Id,
                        Name = $"{product.Name} - {name}",
                        Price = price,
                        ImageUrl = imageUrl,
                        CleanImageUrl = cleanImageUrl, // ✅ ảnh trơn
                        ModelUrl = modelUrl
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "✅ Tạo sản phẩm thành công", productId = product.Id });
        }


        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductUpdateDto dto)
        {
            Console.WriteLine(">>> UPDATE PRODUCT HIT <<<");

            Console.WriteLine(">>> Files trong request = " + Request.Form.Files.Count);
            Console.WriteLine(">>> Keys trong request = " + string.Join(", ", Request.Form.Keys));

            try
            {
                var product = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFound($"Không tìm thấy sản phẩm có ID = {id}");

                product.Name = dto.Name;
                product.Description = dto.Description;
                product.CategoryId = dto.CategoryId;

                Console.WriteLine($"🟡 [UpdateProduct] Tổng file: {Request.Form.Files.Count}");

                // 🖼️ Ảnh đại diện sản phẩm cha
                if (dto.DefaultImage != null)
                {
                    try
                    {
                        using var stream = dto.DefaultImage.OpenReadStream();
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(dto.DefaultImage.FileName, stream),
                            Folder = "uploads/products"
                        };
                        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                        product.ImageUrl = uploadResult.SecureUrl.ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Lỗi upload ảnh đại diện: {ex.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(dto.DefaultImageUrl))
                {
                    product.ImageUrl = dto.DefaultImageUrl;
                }

                // 🗑️ Xóa biến thể
                if (!string.IsNullOrEmpty(dto.DeletedVariantIds))
                {
                    var deletedIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(dto.DeletedVariantIds);
                    if (deletedIds?.Any() == true)
                    {
                        var toRemove = product.Variants.Where(v => deletedIds.Contains(v.Id)).ToList();
                        _context.ProductVariants.RemoveRange(toRemove);
                        Console.WriteLine($"🗑️ Đã xóa {toRemove.Count} biến thể");
                    }
                }

                // 🔄 Thêm hoặc cập nhật biến thể
                if (dto.VariantNames != null && dto.VariantNames.Any())
                {
                    for (int i = 0; i < dto.VariantNames.Count; i++)
                    {
                        var variantId = dto.VariantIds?.ElementAtOrDefault(i);
                        var name = dto.VariantNames[i];
                        var price = dto.VariantPrices?.ElementAtOrDefault(i) ?? 0;

                        // ✅ Lấy file trực tiếp từ form để tránh lỗi index lệch
                        var imageFile = Request.Form.Files.FirstOrDefault(f =>
                            f.Name == $"VariantImages[{i}]" || (i == 0 && f.Name == "VariantImages")
                        );

                        var cleanImageFile = Request.Form.Files.FirstOrDefault(f =>
                            f.Name == $"VariantCleanImages[{i}]" || (i == 0 && f.Name == "VariantCleanImages")
                        );

                        var modelFile = Request.Form.Files.FirstOrDefault(f =>
                            f.Name == $"VariantModels[{i}]" || (i == 0 && f.Name == "VariantModels")
                        );


                        Console.WriteLine($"🧾 VariantCleanImages count: {dto.VariantCleanImages?.Count}");
                        foreach (var f in dto.VariantCleanImages ?? new List<IFormFile>())
                        {
                            Console.WriteLine($"📁 Clean image file: {f.FileName} ({f.Length} bytes)");
                        }


                        string? imageUrl = dto.VariantImageUrls?.ElementAtOrDefault(i);
                        string? cleanImageUrl = dto.VariantCleanImageUrls?.ElementAtOrDefault(i);
                        string? modelUrl = dto.VariantModelUrls?.ElementAtOrDefault(i);

                        ProductVariant variant;
                        if (variantId.HasValue && variantId > 0)
                        {
                            variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
                            if (variant == null) continue;
                        }
                        else
                        {
                            variant = new ProductVariant { ProductId = product.Id };
                            _context.ProductVariants.Add(variant);
                        }

                        variant.Name = $"{product.Name} - {name}";
                        variant.Price = price;

                        // 🖼️ Upload ảnh chính
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            try
                            {
                                using var stream = imageFile.OpenReadStream();
                                var uploadParams = new ImageUploadParams
                                {
                                    File = new FileDescription(imageFile.FileName, stream),
                                    Folder = "uploads/images"
                                };
                                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                                variant.ImageUrl = uploadResult.SecureUrl.ToString();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Lỗi upload ảnh biến thể {i}: {ex.Message}");
                            }
                        }
                        else if (!string.IsNullOrEmpty(imageUrl))
                        {
                            variant.ImageUrl = imageUrl;
                        }

                        // 🖼️ Upload ảnh trơn
                        if (cleanImageFile != null && cleanImageFile.Length > 0)
                        {
                            try
                            {
                                using var stream = cleanImageFile.OpenReadStream();
                                var uploadParams = new ImageUploadParams
                                {
                                    File = new FileDescription(cleanImageFile.FileName, stream),
                                    Folder = "uploads/clean-images"
                                };
                                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                                variant.CleanImageUrl = uploadResult.SecureUrl.ToString();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Lỗi upload ảnh trơn biến thể {i}: {ex.Message}");
                            }
                        }
                        else if (!string.IsNullOrEmpty(cleanImageUrl))
                        {
                            variant.CleanImageUrl = cleanImageUrl;
                        }

                        // 🧱 Upload model 3D
                        if (modelFile != null && modelFile.Length > 0)
                        {
                            try
                            {
                                using var stream = modelFile.OpenReadStream();
                                var uploadParams = new RawUploadParams
                                {
                                    File = new FileDescription(modelFile.FileName, stream),
                                    Folder = "uploads/models"
                                };
                                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                                variant.ModelUrl = uploadResult.SecureUrl.ToString();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Lỗi upload model biến thể {i}: {ex.Message}");
                            }
                        }
                        else if (!string.IsNullOrEmpty(modelUrl))
                        {
                            variant.ModelUrl = modelUrl;
                        }
                    }
                }

                // 🔁 Đồng bộ lại tên biến thể
                foreach (var v in product.Variants)
                {
                    if (!string.IsNullOrEmpty(v.Name))
                    {
                        var parts = v.Name.Split(" - ");
                        v.Name = $"{product.Name} - {parts.Last()}";
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "✅ Cập nhật sản phẩm và biến thể thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Lỗi UpdateProduct: {ex}");
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }






        // 🔴 Xóa sản phẩm cha (xóa luôn biến thể)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            _context.ProductVariants.RemoveRange(product.Variants);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return Ok(new { message = "🗑️ Đã xóa sản phẩm và toàn bộ biến thể." });
        }

        [HttpGet("{id}/engraving")]
        public async Task<IActionResult> GetEngraving(int id)
        {
            var variant = await _context.ProductVariants
                .Select(v => new {
                    v.Id,
                    v.EngravingX,
                    v.EngravingY,
                    v.EngravingColor,
                    v.EngravingFont,
                    v.CleanImageUrl,
                    v.ExtraPrice // ✅ thêm dòng này để FE lấy được phí khắc riêng
                })
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null)
                return NotFound();

            return Ok(variant);
        }

        [HttpPut("{id}/engraving")]
        public async Task<IActionResult> UpdateEngraving(int id, [FromBody] EngravingUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is missing" });

            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null)
                return NotFound(new { message = "Variant not found" });

            // ✅ Cập nhật thông tin khắc
            variant.EngravingX = dto.EngravingX;
            variant.EngravingY = dto.EngravingY;
            variant.EngravingColor = dto.EngravingColor ?? variant.EngravingColor;
            variant.EngravingFont = dto.EngravingFont ?? variant.EngravingFont;

            // ✅ Thêm dòng này để lưu giá khắc riêng
            if (dto.ExtraPrice.HasValue)
                variant.ExtraPrice = dto.ExtraPrice.Value;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Engraving info saved successfully",
                variant = new
                {
                    variant.Id,
                    variant.EngravingX,
                    variant.EngravingY,
                    variant.EngravingColor,
                    variant.EngravingFont,
                    variant.ExtraPrice // ✅ trả luôn giá khắc mới về
                }
            });
        }

        // ✅ Cập nhật DTO để nhận thêm giá khắc
        public class EngravingUpdateDto
        {
            public decimal EngravingX { get; set; }
            public decimal EngravingY { get; set; }
            public string? EngravingColor { get; set; }
            public string? EngravingFont { get; set; }

            // ✅ Thêm giá khắc riêng
            public decimal? ExtraPrice { get; set; }
        }


        [HttpPut("{id}/engraving-text")]
        public async Task<IActionResult> UpdateEngravingText(int id, [FromBody] EngravingTextDto dto)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null) return NotFound();

            variant.EngravingText = dto.EngravingText;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Saved engraving text successfully", variant.Id, variant.EngravingText });
        }

        public class EngravingTextDto
        {
            public string? EngravingText { get; set; }
        }



    }
}
