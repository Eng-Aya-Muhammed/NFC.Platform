namespace NFC.Platform.Domain.Enums
{
    public enum OrderStatus
    {
        AwaitingDesign = 1,
        PendingReview = 2,
        UnderReview = 3,
        Rejected = 4,
        Approved = 5,
        InPrinting = 6,
        Encoding = 7,
        ReadyForDelivery = 8,
        Delivered = 9
    }
}
