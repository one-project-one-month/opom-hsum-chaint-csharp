namespace HsumChaint.Domain.Features.Auth.DTOs
{
    public class LoginRequestDto
    {
        public string? Identifier { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
    }
}
