namespace NFC.Platform.Application.DTOs.CardDesign;

/// <summary>
/// Payload sent by the payment gateway webhook to confirm or fail a payment.
/// Secured via HMAC-SHA256 signature verification in CardDesignService.
/// </summary>
public class PaymentCallbackRequest
{
    /// <summary>Transaction / reference ID assigned by the payment gateway.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>True if payment succeeded; false if it failed or was cancelled.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Human-readable failure reason (populated only when IsSuccess = false).</summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// HMAC-SHA256 signature from the payment gateway used to verify request authenticity.
    /// Verified against the gateway webhook secret stored in appsettings.
    /// </summary>
    public string GatewaySignature { get; set; } = string.Empty;
}
