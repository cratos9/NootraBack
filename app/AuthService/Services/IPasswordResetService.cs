using AuthService.Dtos;

namespace AuthService.Services;

public interface IPasswordResetService
{
    Task RequestResetAsync(PasswordResetRequestDto dto);

    Task ResetPasswordAsync(PasswordResetDto dto);
}