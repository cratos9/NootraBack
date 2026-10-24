using AuthService.Dtos;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginUserDto loginUserDto);
        Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenDto);
        Task LogoutAsync(string accessToken);
        Task<AccountResponseDto> GetAccountAsync(Guid userId);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, string accessToken);
        Task DeleteAccountAsync(Guid userId, DeleteAccountDto deleteAccountDto, string accessToken);
        Task<AccountResponseDto> UpdateAccountAsync(Guid userId, UpdateAccountDto updateAccountDto);
    }
}