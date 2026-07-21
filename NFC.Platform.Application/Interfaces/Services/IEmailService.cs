namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendPasswordResetEmailAsync(string to, string resetLink, string culture);
        Task SendNewUserCredentialsEmailAsync(string to, string username, string password, string culture);
        Task SendOrderReadyOtpEmailAsync(string to, string otp, string cardName, string culture);
        Task SendTemplateRequestApprovedEmailAsync(string to, string templateName, string culture);
    }
}
