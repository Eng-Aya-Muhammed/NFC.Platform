namespace NFC.Platform.Application.DTOs
{
    public class CreateCardRequest
    {
        public string CardNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
