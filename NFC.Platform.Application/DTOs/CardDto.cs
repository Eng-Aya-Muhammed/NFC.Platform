using System;

namespace NFC.Platform.Application.DTOs
{
    public class CardDto
    {
        public Guid Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
