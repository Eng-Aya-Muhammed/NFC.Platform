using System.Globalization;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly IMessageService _messageService;

        public EmailService(IOptions<MailSettings> mailSettings, IMessageService messageService)
        {
            _mailSettings = mailSettings.Value;
            _messageService = messageService;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_mailSettings.From);
            email.Sender.Name = _mailSettings.DisplayName;
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder();
            if (isHtml)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            var secureSocketOption = _mailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, secureSocketOption);
            await smtp.AuthenticateAsync(_mailSettings.UserName, _mailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetLink, string culture)
        {
            SetThreadCulture(culture);

            var subject = _messageService.Get("EmailPasswordResetSubject");
            var body = _messageService.Get("EmailPasswordResetBody", resetLink);

            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendNewUserCredentialsEmailAsync(string to, string username, string password, string culture)
        {
            SetThreadCulture(culture);

            var subject = _messageService.Get("EmailNewUserCredentialsSubject");
            var body = _messageService.Get("EmailNewUserCredentialsBody", username, password);

            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendOrderReadyOtpEmailAsync(string to, string otp, string cardName, string culture)
        {
            SetThreadCulture(culture);

            var isEn = culture?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
            var subject = _messageService.Get("EmailOrderReadySubject");
            var subtitle = _messageService.Get("EmailOrderReadySubtitle");
            var greeting = _messageService.Get("EmailOrderReadyGreeting");
            var bodyText = _messageService.Get("EmailOrderReadyBody", cardName);
            var otpLabel = _messageService.Get("EmailOrderReadyOtpLabel");
            var warningText = _messageService.Get("EmailOrderReadyWarning");
            var footerText = _messageService.Get("EmailOrderReadyFooter");
            var dir = isEn ? "ltr" : "rtl";
            var lang = isEn ? "en" : "ar";

            var body = $@"
<!DOCTYPE html>
<html lang=""{lang}"" dir=""{dir}"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin:0;padding:0;background:#f4f6f9;font-family:Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f6f9;padding:40px 0;"">
    <tr><td align=""center"">
      <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
        <!-- Header -->
        <tr><td style=""background:linear-gradient(135deg,#1a1a2e 0%,#16213e 100%);padding:36px 40px;text-align:center;"">
          <h1 style=""margin:0;color:#ffffff;font-size:22px;font-weight:700;letter-spacing:0.5px;"">NFC Platform</h1>
          <p style=""margin:8px 0 0;color:#a0aec0;font-size:13px;"">{subtitle}</p>
        </td></tr>
        <!-- Body -->
        <tr><td style=""padding:40px;"">
          <p style=""margin:0 0 16px;color:#2d3748;font-size:16px;"">{greeting}</p>
          <p style=""margin:0 0 24px;color:#4a5568;font-size:15px;line-height:1.6;"">
            {bodyText}
          </p>
          <!-- OTP Box -->
          <div style=""background:#f7f8fc;border:2px dashed #e2e8f0;border-radius:12px;padding:28px;text-align:center;margin:0 0 28px;"">
            <p style=""margin:0 0 8px;color:#718096;font-size:13px;text-transform:uppercase;letter-spacing:1px;"">{otpLabel}</p>
            <p style=""margin:0;color:#1a1a2e;font-size:42px;font-weight:800;letter-spacing:10px;font-family:'Courier New',monospace;"">{otp}</p>
          </div>
          <p style=""margin:0;color:#718096;font-size:13px;text-align:center;"">
            {warningText}
          </p>
        </td></tr>
        <!-- Footer -->
        <tr><td style=""background:#f7f8fc;padding:20px 40px;text-align:center;border-top:1px solid #e2e8f0;"">
          <p style=""margin:0;color:#a0aec0;font-size:12px;"">{footerText}</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            await SendEmailAsync(to, subject, body, true);
        }

        private static void SetThreadCulture(string culture)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                var cultureInfo = new CultureInfo(culture);
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;
            }
        }
    }
}
