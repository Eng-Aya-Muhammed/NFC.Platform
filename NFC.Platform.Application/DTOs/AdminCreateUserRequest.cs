using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs
{
    public class AdminCreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public AppRole Role { get; set; }
    }
}
