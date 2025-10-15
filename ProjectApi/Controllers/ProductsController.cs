using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Api.Models;
using ProjectApi.Data;
using ProjectApi.DTOs;
using X.PagedList;

namespace ProjectApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly FurnitureDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(FurnitureDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 🟢 Lấy tất cả sản phẩm (cha + toàn bộ biến thể, có tìm kiếm & phân trang)
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



        // 🟢 Tạo sản phẩm cha + các biến thể
        [HttpPost("create")]
        [RequestSizeLimit(200_000_000)] // ✅ Cho phép file lớn tới 200MB
        [Consumes("multipart/form-data")] // ✅ Bắt Swagger gửi đúng dạng multipart
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

            var images = Request.Form.Files.GetFiles("VariantImages");
            var models = Request.Form.Files.GetFiles("VariantModels");

            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên sản phẩm không được để trống.");

            string uploadPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
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
                    decimal price = dto.VariantPrices?[i] ?? 0;
                    var image = images.ElementAtOrDefault(i);
                    var model = models.ElementAtOrDefault(i);


                    string? imageUrl = null, modelUrl = null;

                    if (image != null)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                        string filePath = Path.Combine(uploadPath, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await image.CopyToAsync(stream);
                        imageUrl = "/uploads/" + fileName;
                    }

                    if (model != null)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(model.FileName);
                        string filePath = Path.Combine(uploadPath, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await model.CopyToAsync(stream);
                        modelUrl = "/uploads/" + fileName;
                    }

                    _context.ProductVariants.Add(new ProductVariant
                    {
                        ProductId = product.Id,
                        Name = $"{product.Name} - {name}",
                        Price = price,
                        ImageUrl = imageUrl,
                        ModelUrl = modelUrl
                    });
                }

                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "✅ Tạo sản phẩm thành công", productId = product.Id });
        }

        // 🟡 Cập nhật sản phẩm cha + đồng bộ biến thể
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductUpdateDto dto)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound($"Không tìm thấy sản phẩm có ID = {id}");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.CategoryId = dto.CategoryId;

            string uploadPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            Console.WriteLine($"[UpdateProduct] ContentType: {Request.ContentType}");
            Console.WriteLine($"[UpdateProduct] Files count: {Request.Form.Files.Count}");
            foreach (var f in Request.Form.Files)
                Console.WriteLine($"[UpdateProduct] File field: '{f.Name}' FileName: '{f.FileName}'");

            // ✅ Xóa biến thể nếu có DeletedVariantIds
            if (!string.IsNullOrEmpty(dto.DeletedVariantIds))
            {
                var deletedIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(dto.DeletedVariantIds);
                if (deletedIds != null && deletedIds.Any())
                {
                    var toRemove = product.Variants.Where(v => deletedIds.Contains(v.Id)).ToList();
                    _context.ProductVariants.RemoveRange(toRemove);
                }
            }

            // ✅ Cập nhật hoặc thêm biến thể
            if (dto.VariantNames != null && dto.VariantNames.Any())
            {
                for (int i = 0; i < dto.VariantNames.Count; i++)
                {
                    var variantId = dto.VariantIds?.ElementAtOrDefault(i);
                    var name = dto.VariantNames[i];
                    var price = dto.VariantPrices?[i] ?? 0;

                    // ✅ Lấy file chính xác theo key FormData (VariantImages[0], VariantModels[0], ...)
                    var imageFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"VariantImages[{i}]");
                    var modelFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"VariantModels[{i}]");

                    var imageUrl = dto.VariantImageUrls?.ElementAtOrDefault(i);
                    var modelUrl = dto.VariantModelUrls?.ElementAtOrDefault(i);

                    ProductVariant variant;
                    if (variantId != null && variantId > 0)
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
                        if (variant == null)
                            continue;
                    }
                    else
                    {
                        variant = new ProductVariant { ProductId = product.Id };
                        _context.ProductVariants.Add(variant); // ✅ thay vì product.Variants.Add
                    }


                    variant.Name = $"{product.Name} - {name}";
                    variant.Price = price;

                    // ✅ Nếu có ảnh mới → ghi đè ảnh cũ
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                        string path = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(path, FileMode.Create))
                            await imageFile.CopyToAsync(stream);
                        variant.ImageUrl = "/uploads/" + fileName;
                    }
                    else if (!string.IsNullOrEmpty(imageUrl))
                    {
                        variant.ImageUrl = imageUrl;
                    }

                    // ✅ Nếu có model mới → ghi đè model cũ
                    if (modelFile != null && modelFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(modelFile.FileName);
                        string path = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(path, FileMode.Create))
                            await modelFile.CopyToAsync(stream);
                        variant.ModelUrl = "/uploads/" + fileName;
                    }
                    else if (!string.IsNullOrEmpty(modelUrl))
                    {
                        variant.ModelUrl = modelUrl;
                    }
                }

                // Đồng bộ lại tên biến thể
                foreach (var v in product.Variants)
                {
                    if (!string.IsNullOrEmpty(v.Name))
                    {
                        var parts = v.Name.Split(" - ");
                        v.Name = $"{product.Name} - {parts.Last()}";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok("✅ Cập nhật sản phẩm và biến thể thành công!");
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
    }
}
