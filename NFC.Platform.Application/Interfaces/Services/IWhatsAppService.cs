namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Contract for sending WhatsApp messages via an external provider (Twilio).
    /// </summary>
    public interface IWhatsAppService
    {
        /// <summary>
        /// Sends a WhatsApp message to the specified phone number.
        /// </summary>
        /// <param name="toPhoneNumber">Recipient phone number in E.164 format (e.g. +96512345678).</param>
        /// <param name="message">The message body to send.</param>
        Task SendWhatsAppMessageAsync(string toPhoneNumber, string message);
    }
}
