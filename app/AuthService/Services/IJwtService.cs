using AuthService.Entities;

namespace AuthService.Services
{
    public sealed record AccessTokenResult(
        string Token,
        DateTime Expiration
    );

    public interface IJwtService
    {
        AccessTokenResult GenerateAccessToken(UserEntity user);
        string GenerateRefreshToken();
    }
}