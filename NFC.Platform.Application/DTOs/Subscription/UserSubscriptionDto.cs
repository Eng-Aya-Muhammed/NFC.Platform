using System;

namespace NFC.Platform.Application.DTOs
{
    public class UserSubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RemainingDays { get; set; }
        public bool IsActive { get; set; }
    }
}
