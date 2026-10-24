using AuthService.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace AuthService.Services
{
    public class ProfilePictureService : IProfilePictureService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;
        private const int MaxImageDimension = 6000;
        private const long MaxImagePixels = 25_000_000;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfilePictureService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<string> UploadAsync(Guid userId, IFormFile file)
        {
            if (file is null || file.Length == 0)
            {
                throw new ArgumentException("You must provide a valid image file.");
            }

            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException("The image cannot exceed 5 MB.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is null)
            {
                throw new KeyNotFoundException("The account does not exist or is inactive.");
            }

            using var inputStream = file.OpenReadStream();
            using var codec = SKCodec.Create(inputStream);
            if (codec is null)
            {
                throw new ArgumentException("The file does not contain a compatible image.");
            }

            var imageInfo = codec.Info;
            if (imageInfo.Width <= 0 || imageInfo.Height <= 0 || imageInfo.Width > MaxImageDimension || imageInfo.Height > MaxImageDimension || (long)imageInfo.Width * imageInfo.Height > MaxImagePixels)
            {
                throw new ArgumentException("The image has invalid dimensions or is too large.");
            }

            using var bitmap = SKBitmap.Decode(codec);
            if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                throw new ArgumentException("The image could not be decoded or is invalid.");
            }

            var uploadDirectory = GetUploadDirectory();
            Directory.CreateDirectory(uploadDirectory);
            var fileName = $"{userId}.png";
            var finalPath = Path.Combine(uploadDirectory, fileName);
            var temporaryPath = Path.Combine(uploadDirectory, $"{Guid.NewGuid():N}.tmp");

            try
            {
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                if (data is null || data.Size == 0)
                {
                    throw new InvalidOperationException("The image could not be converted to PNG.");
                }

                await File.WriteAllBytesAsync(temporaryPath, data.ToArray());
                File.Move(temporaryPath, finalPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            user.ProfilePictureUrl = $"/uploads/profile-pictures/{fileName}";
            await _context.SaveChangesAsync();
            return user.ProfilePictureUrl;
        }

        public async Task<string> GetCurrentPictureUrlAsync(Guid userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is null)
            {
                throw new KeyNotFoundException("The account does not exist or is inactive.");
            }

            if (string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
            {
                return "/images/default.png";
            }

            return user.ProfilePictureUrl;
        }

        public async Task DeleteAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is null)
            {
                throw new KeyNotFoundException("The account does not exist or is inactive.");
            }

            if (string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
            {
                return;
            }

            user.ProfilePictureUrl = null;
            await _context.SaveChangesAsync();

            var filePath = Path.Combine(GetUploadDirectory(), $"{userId}.png");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string GetUploadDirectory()
        {
            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            return Path.Combine(webRoot, "uploads", "profile-pictures");
        }
    }
}