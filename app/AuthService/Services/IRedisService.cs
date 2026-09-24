namespace AuthService.Services.Redis
{
    public interface IRedisService
    {
        Task<bool> SetAsync(string key, string value, TimeSpan expiration);
        Task<string?> GetAsync(string key);
        Task<bool> DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}