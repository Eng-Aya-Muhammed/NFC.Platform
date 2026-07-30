using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Auth
{
    public class GoogleRegisterRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public AccountType AccountType { get; set; } = AccountType.Individual;
        public string? CompanyName { get; set; }
        public string? WhatsApp { get; set; }
    }
}
