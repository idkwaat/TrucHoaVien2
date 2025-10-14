using System.Collections.Generic;

namespace ProjectApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";

        public string? AvatarUrl { get; set; }   // ảnh đại diện
        public string? Phone { get; set; }       // số điện thoại
        public string? Address { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new();
        public string Password { get; set; } = string.Empty;
    }
}
