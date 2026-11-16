using Microsoft.AspNetCore.Http;

namespace MensajeService.Services;

public class FileStorageService
{
    private readonly string _rootPath;

    public FileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["FileStorage:RootPath"] ?? "App_Data/Uploads";
        _rootPath = Path.GetFullPath(Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task<string> SaveAsync(IFormFile file, string storedFilename, CancellationToken cancellationToken = default)
    {
        var relativePath = Path.Combine("messages", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), storedFilename);
        var fullPath = Path.Combine(_rootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        Directory.CreateDirectory(directory);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        await file.CopyToAsync(stream, cancellationToken);

        return relativePath.Replace('\\', '/');
    }

    public FileStream OpenRead(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedPath));
        var root = _rootPath.EndsWith(Path.DirectorySeparatorChar) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid file path.");
        }

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Attachment file not found.");

        return new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );
    }

    public Task DeleteAsync(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var root = Path.GetFullPath(_rootPath);
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid file path.");
        }

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}