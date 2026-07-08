using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxEmployees { get; set; }
    }
}
