using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Auth;

public class RegisterRequest
{
    public string? Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public AccountType AccountType { get; set; } = AccountType.Individual;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? IndustryType { get; set; }
    public CompanySize? CompanySize { get; set; }
    public string? CommercialRegistrationNumber { get; set; }
}


