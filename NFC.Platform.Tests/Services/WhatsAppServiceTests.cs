using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NFC.Platform.BuildingBlocks.Settings;
using NFC.Platform.Infrastructure.Services;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class WhatsAppServiceTests
    {
        [Fact(Skip = "Manual live integration test — requires active Twilio credentials and network")]
        public async Task SendWhatsAppMessageAsync_SendsLiveWhatsAppMessage_ToUserPhoneNumber()
        {
            // Arrange — Live Twilio credentials configured in appsettings
            var settings = Options.Create(new TwilioSettings
            {
                AccountSid   = "YOUR_TWILIO_ACCOUNT_SID",
                AuthToken    = "YOUR_TWILIO_AUTH_TOKEN",
                WhatsAppFrom = "whatsapp:+14155238886"
            });

            var whatsAppService = new WhatsAppService(settings);
            var recipientNumber = "+201013503890";
            var message = "اختبار حي 🎉 طلبك جاهز للاستلام! كود التحقق الخاص بك هو: *749201*";

            // Act — Call the real Twilio API to send live WhatsApp message
            await whatsAppService.SendWhatsAppMessageAsync(recipientNumber, message);

            // Assert
            Assert.True(true);
        }
    }
}
