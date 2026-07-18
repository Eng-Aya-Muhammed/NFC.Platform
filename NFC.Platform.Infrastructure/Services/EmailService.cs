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
