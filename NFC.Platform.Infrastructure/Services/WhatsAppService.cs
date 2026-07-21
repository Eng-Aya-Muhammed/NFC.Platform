using Microsoft.Extensions.Options;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Settings;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NFC.Platform.Infrastructure.Services
{
    /// <summary>
    /// Sends WhatsApp messages via the Twilio API.
    /// Uses the Twilio Sandbox number during development/testing.
    /// </summary>
    public class WhatsAppService : IWhatsAppService
    {
        private readonly TwilioSettings _settings;

        public WhatsAppService(IOptions<TwilioSettings> settings)
        {
            _settings = settings.Value;
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }

        /// <inheritdoc/>
        public async Task SendWhatsAppMessageAsync(string toPhoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(toPhoneNumber))
                return;

            var formattedNumber = NormalizePhoneNumber(toPhoneNumber);
            var to = $"whatsapp:{formattedNumber}";

            await MessageResource.CreateAsync(
                to: new PhoneNumber(to),
                from: new PhoneNumber(_settings.WhatsAppFrom),
                body: message);
        }

        private static string NormalizePhoneNumber(string input)
        {
            var clean = input.Trim();
            if (clean.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring("whatsapp:".Length).Trim();
            }

            // Remove spaces, dashes, parentheses
            clean = clean.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            return clean;
        }
    }
}
