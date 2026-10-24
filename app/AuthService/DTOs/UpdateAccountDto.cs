namespace AuthService.Dtos
{
    public class UpdateAccountDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Biography { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Institution { get; set; }
        public string? Career { get; set; }
        public string? StudentId { get; set; }
    }
}