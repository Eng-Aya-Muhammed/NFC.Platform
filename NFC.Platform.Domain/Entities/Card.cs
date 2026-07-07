using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class Card : BaseEntity
    {
        public string CardNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public decimal Price { get; set; }
    }
}
