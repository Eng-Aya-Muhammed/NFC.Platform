namespace NFC.Platform.Application.DTOs.CardDesign;

public class PaymentCallbackRequest
{
    public string TransactionId { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }

    public string? FailureReason { get; set; }

    public string GatewaySignature { get; set; } = string.Empty;
}
