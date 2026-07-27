using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class CardPackage : BaseEntity
    {
        public int NumberOfCards { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
