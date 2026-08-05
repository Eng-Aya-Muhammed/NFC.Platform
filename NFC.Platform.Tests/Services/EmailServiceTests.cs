namespace NFC.Platform.Tests.Services
{
    public class EmailServiceTests
    {
        private readonly IMessageService _messageService;
        private readonly IOptions<MailSettings> _mailSettingsOptions;
        private readonly MailSettings _mailSettings;

        public EmailServiceTests()
        {
            _messageService = Substitute.For<IMessageService>();

            _mailSettings = new MailSettings
            {
                From = "noreply@nfcplatform.com",
                DisplayName = "NFC Platform Test",
                Host = "sandbox.smtp.mailtrap.io",
                Port = 2525,
                UserName = "7ce491d5857d5e",
                Password = "721ce947dbc25d",
                EnableSsl = true
            };

            _mailSettingsOptions = Substitute.For<IOptions<MailSettings>>();
            _mailSettingsOptions.Value.Returns(_mailSettings);
        }

        [Fact(Skip = "Requires external Mailtrap SMTP server connection")]
        public async Task SendEmails_SendsRealEmailsToMailtrapSequentially()
        {
            var realEmailService = new EmailService(_mailSettingsOptions, _messageService);

            var ex1 = await Record.ExceptionAsync(() =>
                realEmailService.SendEmailAsync(
                    to: "test-receiver@nfcplatform.com",
                    subject: "NFC Platform Integration Test - Basic Email",
                    body: "<h3>Integration Test Success</h3><p>This is a real basic email sent during the automated test execution.</p>",
                    isHtml: true
                )
            );
            Assert.Null(ex1);
        }
    }
}
