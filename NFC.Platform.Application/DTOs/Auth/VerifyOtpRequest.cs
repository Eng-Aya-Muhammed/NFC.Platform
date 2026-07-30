namespace NFC.Platform.Application.DTOs.Auth
{
    public class VerifyOtpRequest
    {
        public string? Email { get; set; }
        public string OtpCode { get; set; } = string.Empty;
    }
}
