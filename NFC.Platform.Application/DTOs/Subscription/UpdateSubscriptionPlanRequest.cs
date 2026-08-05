namespace NFC.Platform.Application.DTOs.Subscription
{
    public class UpdateSubscriptionPlanRequest
    {
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public List<string>? Features { get; set; }
        public decimal? Price { get; set; }
        public int? DurationInDays { get; set; }


        public int? MaxTemplateChanges { get; set; }

        public int? MaxCustomDesignRequests { get; set; }

        public List<Guid>? TemplateIds { get; set; }
    }
}
