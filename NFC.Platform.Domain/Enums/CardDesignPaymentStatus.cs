using System.Text.Json.Serialization;

namespace NFC.Platform.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CardDesignPaymentStatus
    {
        Pending = 1,
        Paid = 2,
        Failed = 3,
        Refunded = 4
    }
}
