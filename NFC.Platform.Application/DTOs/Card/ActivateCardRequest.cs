using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Application.DTOs.Card;

    /// <summary>
    /// Request parameters for activating an NFC card.
    /// </summary>
    public class ActivateCardRequest
    {
        /// <summary>
        /// Gets or sets the unique activation code printed on the card.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string ActivationCode { get; set; } = string.Empty;
    }

