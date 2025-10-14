using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
using ProjectApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly FurnitureDbContext _context;

        public CategoriesController(FurnitureDbContext context)
        {
            _context = context;
        }

        // ✅ Lấy tất cả danh mục (kèm số sản phẩm)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var categories = await _context.Categories
                .Include(c => c.Products) // lấy danh sách sản phẩm trong từng danh mục
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ProductCount = c.Products.Count // ✅ đếm số sản phẩm
                })
                .ToListAsync();

            return Ok(categories);
        }


        // ✅ Lấy danh mục theo id
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetById(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // ✅ Tạo mới danh mục
        [HttpPost]
        public async Task<ActionResult<Category>> Create([FromBody] Category category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name))
                return BadRequest("Tên danh mục không được để trống.");

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        // ✅ Cập nhật danh mục
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Category category)
        {
            if (id != category.Id)
                return BadRequest("ID không trùng khớp.");

            var existing = await _context.Categories.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Name = category.Name;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ✅ Xóa danh mục
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { message = "Không tìm thấy danh mục." });

            int productCount = category.Products?.Count ?? 0;
            if (productCount > 0)
            {
                return BadRequest(new
                {
                    message = $"Không thể xóa vì có {productCount} sản phẩm đang thuộc danh mục này.",
                    productCount
                });
            }


            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa danh mục thành công!" });
        }


    }
}
