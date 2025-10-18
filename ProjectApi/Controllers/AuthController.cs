using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
using ProjectApi.Dtos;
using ProjectApi.Models;
using ProjectApi.Services;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FurnitureDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(FurnitureDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username already exists");

            var role = string.IsNullOrEmpty(dto.Role) ? "User" : dto.Role;

            // ✅ Chặn người bình thường tự tạo Admin
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Ví dụ: chỉ cho phép tạo admin nếu chưa có admin nào
                bool hasAdmin = await _context.Users.AnyAsync(u => u.Role == "Admin");
                if (hasAdmin)
                    return BadRequest("Bạn không thể tự tạo tài khoản admin!");
            }

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Phone = dto.Phone,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = role
            };



            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User created successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Vui lòng nhập đầy đủ thông tin đăng nhập");

            // ✅ Cho phép đăng nhập bằng Username, Email hoặc Phone
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == dto.Username ||
                u.Email == dto.Username ||
                u.Phone == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Sai tài khoản hoặc mật khẩu");

            var token = _tokenService.CreateAccessToken(user);

            return Ok(new { token });
        }


    }
}
