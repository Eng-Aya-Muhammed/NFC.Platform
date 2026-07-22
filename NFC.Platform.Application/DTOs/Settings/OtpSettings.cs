namespace NFC.Platform.Application.DTOs.Settings;

public class OtpSettings
{
    public int CooldownSeconds { get; set; } = 60;
    public int MaxResendAttempts { get; set; } = 5;
}
