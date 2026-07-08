using System;

namespace NFC.Platform.Application.DTOs
{
    public class CardDto
    {
        public Guid Id { get; set; }
        public string ActivationCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public Guid? UserProfileId { get; set; }
        public Guid? CardOrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
