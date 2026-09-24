using AuthService.Dtos;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginUserDto loginUserDto);
        Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenDto);
        Task LogoutAsync(string refreshToken);
    }
}