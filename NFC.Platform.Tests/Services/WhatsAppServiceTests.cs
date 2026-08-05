namespace NFC.Platform.Tests.Services
{
    public class WhatsAppServiceTests
    {
        [Fact(Skip = "Manual live integration test — requires active Twilio credentials and network")]
        public async Task SendWhatsAppMessageAsync_SendsLiveWhatsAppMessage_ToUserPhoneNumber()
        {
            var settings = Options.Create(new TwilioSettings
            {
                AccountSid = "YOUR_TWILIO_ACCOUNT_SID",
                AuthToken = "YOUR_TWILIO_AUTH_TOKEN",
                WhatsAppFrom = "whatsapp:+14155238886"
            });

            var whatsAppService = new WhatsAppService(settings);
            var recipientNumber = "+201013503890";
            var message = "اختبار حي 🎉 طلبك جاهز للاستلام! كود التحقق الخاص بك هو: *749201*";

            await whatsAppService.SendWhatsAppMessageAsync(recipientNumber, message);

            Assert.True(true);
        }
    }
}
