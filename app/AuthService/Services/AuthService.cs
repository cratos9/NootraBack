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

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("This account is inactive.");
            }

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();
            var now = DateTime.UtcNow;

            user.LastLogin = now;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryAt = now.AddDays(7);

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

            var usernameExists = await _context.Users.AnyAsync(u => u.Username == createUserDto.Username);
            if (usernameExists)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            var now = DateTime.UtcNow;

            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                FullName = createUserDto.FullName,
                CreatedAt = now,
                IsActive = true,
                IsVerified = false,
                IsEmailVerified = false
            };

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryAt = now.AddDays(7);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return BuildAuthResponse(user, accessToken, refreshToken);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequestDto)
        {
            var currentRefreshToken = refreshTokenRequestDto.RefreshToken;
            var now = DateTime.UtcNow;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == currentRefreshToken);
            if (user == null || !user.IsActive || !user.RefreshTokenExpiryAt.HasValue || user.RefreshTokenExpiryAt.Value <= now)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            var accessToken = _jwt.GenerateAccessToken(user);
            var newRefreshToken = _jwt.GenerateRefreshToken();
            var newRefreshTokenExpiry = now.AddDays(7);

            var updatedRows = await _context.Users
                .Where(u => u.Id == user.Id && u.IsActive && u.RefreshToken == currentRefreshToken && u.RefreshTokenExpiryAt.HasValue && u.RefreshTokenExpiryAt.Value > now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.RefreshToken, newRefreshToken)
                    .SetProperty(u => u.RefreshTokenExpiryAt, newRefreshTokenExpiry));
            if (updatedRows == 0)
            {
                throw new UnauthorizedAccessException("Invalid or already used refresh token.");
            }

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryAt = newRefreshTokenExpiry;

            return BuildAuthResponse(user, accessToken, newRefreshToken);
        }

        public async Task LogoutAsync(string accessToken)
        {
            JwtSecurityToken jwt;

            try
            {
                jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException)
            {
                throw new UnauthorizedAccessException("Invalid access token.");
            }

            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var userIdValue = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(jti) || !Guid.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedAccessException("Access token is missing required claims.");
            }

            var expiration = jwt.ValidTo - DateTime.UtcNow;
            if (expiration <= TimeSpan.Zero)
            {
                throw new UnauthorizedAccessException("Access token has expired.");
            }

            var blacklistSaved = await _redisService.SetAsync($"blacklist:{jti}", "revoked", expiration);
            if (!blacklistSaved)
            {
                throw new InvalidOperationException("Could not revoke the access token.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryAt = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<AccountResponseDto> GetAccountAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return new AccountResponseDto
            {
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Biography = user.Biography,
                ProfilePictureUrl = user.ProfilePictureUrl,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                City = user.City,
                Institution = user.Institution,
                Career = user.Career,
                StudentId = user.StudentId,
                IsActive = user.IsActive,
                IsVerified = user.IsVerified,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLogin = user.LastLogin
            };
        }

        private static AuthResponseDto BuildAuthResponse(UserEntity user, AccessTokenResult accessToken, string refreshToken)
        {
            return new AuthResponseDto
            {
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName
                },
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken,
                Expiration = accessToken.Expiration
            };
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, string accessToken)
        {       
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is null)
            {
                throw new KeyNotFoundException("The user was not found or is inactive.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("The current password is incorrect.");
            }

            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
            {
                throw new ArgumentException("The new password must be different from the current password.");
            }

            await LogoutAsync(accessToken);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            user.RefreshToken = null;
            user.RefreshTokenExpiryAt = null;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAccountAsync(Guid userId, DeleteAccountDto dto, string accessToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is null)
            {
                throw new KeyNotFoundException("The account does not exist or has already been deleted.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("The password is incorrect.");
            }

            await LogoutAsync(accessToken);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<AccountResponseDto> UpdateAccountAsync(Guid userId, UpdateAccountDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is null)
            {
                throw new KeyNotFoundException("The account does not exist or is inactive.");
            }

            if (dto.Username is not null && !string.Equals(user.Username, dto.Username, StringComparison.Ordinal))
            {
                var usernameExists = await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != userId);
                if (usernameExists)
                {
                    throw new InvalidOperationException("The username is already in use.");
                }
                user.Username = dto.Username;
            }

            if (dto.Email is not null && !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId);
                if (emailExists)
                {
                    throw new InvalidOperationException("The email is already registered.");
                }

                user.Email = dto.Email;
            }

            if (dto.FullName is not null)user.FullName = dto.FullName;
            if (dto.Biography is not null)user.Biography = dto.Biography;
            if (dto.ProfilePictureUrl is not null)user.ProfilePictureUrl = dto.ProfilePictureUrl;
            if (dto.PhoneNumber is not null)user.PhoneNumber = dto.PhoneNumber;
            if (dto.Country is not null)user.Country = dto.Country;
            if (dto.City is not null)user.City = dto.City;
            if (dto.Institution is not null)user.Institution = dto.Institution;
            if (dto.Career is not null)user.Career = dto.Career;
            if (dto.StudentId is not null)user.StudentId = dto.StudentId;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new AccountResponseDto
            {
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Biography = user.Biography,
                ProfilePictureUrl = user.ProfilePictureUrl,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                City = user.City,
                Institution = user.Institution,
                Career = user.Career,
                StudentId = user.StudentId,
                IsActive = user.IsActive,
                IsVerified = user.IsVerified,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLogin = user.LastLogin
            };
        }
    }
}