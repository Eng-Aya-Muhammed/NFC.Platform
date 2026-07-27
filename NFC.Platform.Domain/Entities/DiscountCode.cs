using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class DiscountCode : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
