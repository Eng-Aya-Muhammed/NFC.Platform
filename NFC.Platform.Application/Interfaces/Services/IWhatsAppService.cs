namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IWhatsAppService
    {
        Task SendWhatsAppMessageAsync(string toPhoneNumber, string message);
    }
}
