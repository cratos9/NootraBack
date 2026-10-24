using Microsoft.AspNetCore.Http;

namespace AuthService.Services
{
    public interface IProfilePictureService
    {
        Task<string> UploadAsync(Guid userId, IFormFile file);

        Task<string> GetCurrentPictureUrlAsync(Guid userId);

        Task DeleteAsync(Guid userId);
    }
}