namespace Charter.ReporterApp.Application.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
    
    public class LoginResultDto
    {
        public bool Success { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }
        public string? ErrorMessage { get; set; }
    }
}