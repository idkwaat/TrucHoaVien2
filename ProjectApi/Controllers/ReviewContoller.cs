using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
using ProjectApi.Models;
using ProjectApi.Dtos;
using System.Security.Claims;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly FurnitureDbContext _context;

        public ReviewsController(FurnitureDbContext context)
        {
            _context = context;
        }

        // ✅ Gửi review
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PostReview([FromBody] ReviewCreateDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid user ID");

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be 1-5");

            var review = new Review
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review created", review });
        }

        // ✅ Review của user hiện tại
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyReviews()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid user ID");

            var reviews = await _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        // ✅ Review theo product
        [AllowAnonymous]
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.ProductId,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    User = r.User == null ? null : new
                    {
                        r.User.Id,
                        Name =  r.User.Username,
                        AvatarUrl = r.User.AvatarUrl
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }

    }
}
