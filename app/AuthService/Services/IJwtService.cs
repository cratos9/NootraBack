using AuthService.Entities;

namespace AuthService.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(UserEntity user);
        string GenerateRefreshToken();
    }
}