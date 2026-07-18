using System;

namespace NFC.Platform.Application.DTOs.Card;

    public class CardDto
    {
        public Guid Id { get; set; }
        public string ActivationCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public Guid? UserProfileId { get; set; }
        public Guid? CardOrderId { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Cloudinary URL of the QR code image for this card.
        /// Points to the same destination as the NFC chip (ProfileUrl).
        /// </summary>
        public string? QrCodeUrl { get; set; }
    }

