namespace NFC.Platform.BuildingBlocks.Settings
{
    public class TwilioSettings
    {
        /// <summary>
        /// Twilio Account SID from the Twilio Console.
        /// </summary>
        public string AccountSid { get; set; } = string.Empty;

        /// <summary>
        /// Twilio Auth Token from the Twilio Console.
        /// </summary>
        public string AuthToken { get; set; } = string.Empty;

        /// <summary>
        /// The WhatsApp-enabled sender number in E.164 format prefixed with "whatsapp:".
        /// Example: "whatsapp:+14155238886" for the Twilio Sandbox.
        /// </summary>
        public string WhatsAppFrom { get; set; } = string.Empty;
    }
}
