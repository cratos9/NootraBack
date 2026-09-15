namespace AuthService.Entities
{
    public class UserEntity
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string FullName { get; set; }
        public string? Biography { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Institution { get; set; }
        public string? Career { get; set; }
        public string? StudentId { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryAt { get; set; }
    }
}