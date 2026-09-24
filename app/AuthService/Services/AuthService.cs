using Microsoft.EntityFrameworkCore;
using AuthService.Dtos;
using AuthService.Data;
using AuthService.Entities;
using AuthService.Services.Redis;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IRedisService _redisService;
        private readonly IJwtService _jwt;

        public AuthService(AppDbContext context, IRedisService redisService, IJwtService jwt)
        {
            _context = context;
            _redisService = redisService;
            _jwt = jwt;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginUserDto loginUserDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginUserDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginUserDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            user.LastLogin = DateTime.UtcNow;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return BuildAuthResponse(user, accessToken, refreshToken);
        }

        public async Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == createUserDto.Email);
            if (emailExists)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var useranameExists = await _context.Users.AnyAsync(u => u.Username == createUserDto.Username);
            if (useranameExists)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                FullName = createUserDto.FullName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsVerified = false,
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return BuildAuthResponse(user, accessToken, refreshToken);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequestDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenRequestDto.RefreshToken);
            if (user == null || user.RefreshTokenExpiryAt < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            var accessToken = _jwt.GenerateAccessToken(user);
            var newRefreshToken = _jwt.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return BuildAuthResponse(user, accessToken, newRefreshToken);
        }

        private AuthResponseDto BuildAuthResponse(UserEntity user, string accessToken, string refreshToken)
        {
            return new AuthResponseDto
            {
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                },
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(60)
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(refreshToken);
            var jti = jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            var expiration = jwt.ValidTo - DateTime.UtcNow;
            await _redisService.SetAsync($"blacklist:{jti}", "revoked", expiration);
            var userId = jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryAt = null;
                await _context.SaveChangesAsync();
            }
        }
    }
}