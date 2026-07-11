using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Auth;

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public AccountType AccountType { get; set; } = AccountType.Individual;
        public string? CompanyName { get; set; }
    }


