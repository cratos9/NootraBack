using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services
{
    public class JwtService : IJwtService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly double _durationInMinutes;

        public JwtService(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"]?? throw new InvalidOperationException("JWT signing key is not configured.");
            _issuer = configuration["Jwt:Issuer"]?? throw new InvalidOperationException("JWT issuer is not configured.");
            _audience = configuration["Jwt:Audience"]?? throw new InvalidOperationException("JWT audience is not configured.");

            if (!double.TryParse(configuration["Jwt:DurationInMinutes"], out _durationInMinutes) || _durationInMinutes <= 0)
            {
                throw new InvalidOperationException("JWT duration must be a positive number.");
            }

            if (Encoding.UTF8.GetByteCount(_key) < 32)
            {
                throw new InvalidOperationException("JWT signing key must contain at least 32 bytes.");
            }
        }

        public AccessTokenResult GenerateAccessToken(UserEntity user)
        {
            var now = DateTime.UtcNow;
            var expiration = now.AddMinutes(_durationInMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName,user.Username),
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: expiration,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return new AccessTokenResult(tokenString, expiration);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }
    }
}