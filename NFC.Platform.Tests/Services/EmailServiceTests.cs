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
            
            // Real Mailtrap credentials provided by the user
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
            // Arrange
            var realEmailService = new EmailService(_mailSettingsOptions, _messageService);

            // Test 1: SendEmailAsync
            var ex1 = await Record.ExceptionAsync(() => 
                realEmailService.SendEmailAsync(
                    to: "test-receiver@nfcplatform.com", 
                    subject: "NFC Platform Integration Test - Basic Email", 
                    body: "<h3>Integration Test Success</h3><p>This is a real basic email sent during the automated test execution.</p>", 
                    isHtml: true
                )
            );
            Assert.Null(ex1);

            // Wait 10 seconds to respect Mailtrap free tier rate limits (too many emails per second)
            await Task.Delay(10000);

            // Test 2: SendPasswordResetEmailAsync (Arabic Template)
            var resetLink = "http://localhost:3000/reset-password?token=xyz";
            _messageService.Get("EmailPasswordResetSubject").Returns("منصة NFC - إعادة تعيين كلمة المرور");
            _messageService.Get("EmailPasswordResetBody", resetLink).Returns("<p>رابط تعيين الباسورد الخاص بك</p>");

            var ex2 = await Record.ExceptionAsync(() => 
                realEmailService.SendPasswordResetEmailAsync("user@test.com", resetLink, "ar")
            );
            Assert.Null(ex2);

            // Wait 10 seconds to respect Mailtrap free tier rate limits
            await Task.Delay(10000);

            // Test 3: SendNewUserCredentialsEmailAsync (English Template)
            var username = "newuser";
            var password = "TempPassword123!";
            _messageService.Get("EmailNewUserCredentialsSubject").Returns("NFC Platform - Your Credentials");
            _messageService.Get("EmailNewUserCredentialsBody", username, password).Returns("<p>Your temp password credentials</p>");

            var ex3 = await Record.ExceptionAsync(() => 
                realEmailService.SendNewUserCredentialsEmailAsync("newuser@test.com", username, password, "en")
            );
            Assert.Null(ex3);
        }
    }
}
