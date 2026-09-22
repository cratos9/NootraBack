namespace AuthService.Dtos
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public UserResponseDto User { get; set; } = new();
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public DateTime Expiration { get; set; }
    }

    public class RefreshTokenResponseDto
    {
        public string RefreshToken { get; set; } = default!;
    }
}