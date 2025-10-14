using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ProjectApi.Models;

namespace ProjectApi.Services
{
    public interface ITokenService
    {
        string CreateAccessToken(User user);
        (string token, DateTime expiresAt) CreateRefreshToken();
        string HashToken(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly byte[] _key;

        public TokenService(IConfiguration config)
        {
            _config = config;
            _key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
        }

        public string CreateAccessToken(User user)
        {
            var jwtCfg = _config.GetSection("Jwt");

            var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),   // giữ nguyên
    new Claim(ClaimTypes.Role, user.Role),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};


            var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtCfg["Issuer"],
                audience: jwtCfg["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtCfg["AccessTokenExpirationMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public (string token, DateTime expiresAt) CreateRefreshToken()
        {
            var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var expiresAt = DateTime.UtcNow.AddDays(double.Parse(_config["Jwt:RefreshTokenExpirationDays"]));
            return (random, expiresAt);
        }

        public string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
