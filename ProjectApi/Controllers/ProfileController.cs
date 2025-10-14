using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
using ProjectApi.Models;
using ProjectApi.Dtos;
using System.Security.Claims;
using BCrypt.Net;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly FurnitureDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(FurnitureDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ Lấy thông tin người dùng hiện tại
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid user ID");

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found");

            var result = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                AvatarUrl = user.AvatarUrl ?? "",
                user.Phone,
                user.Address
            };

            return Ok(result);
        }

        // ✅ Cập nhật thông tin
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid user ID");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            user.Username = dto.Username ?? user.Username;
            user.Email = dto.Email ?? user.Email;
            user.Phone = dto.Phone ?? user.Phone;
            user.Address = dto.Address ?? user.Address;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully" });
        }

        // ✅ Đổi mật khẩu
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid user ID");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
                return BadRequest("Mật khẩu hiện tại không đúng");

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password changed successfully" });
        }

        // ✅ Upload ảnh đại diện
        [HttpPost("avatar")]
        [RequestSizeLimit(10_000_000)] // 10MB
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest("File type not allowed");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid user ID");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "avatars");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/avatars/{fileName}";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { avatarUrl = user.AvatarUrl });
        }
    }
}
