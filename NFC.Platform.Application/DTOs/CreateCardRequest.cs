using System;

namespace NFC.Platform.Application.DTOs
{
    public class CreateCardRequest
    {
        public string ActivationCode { get; set; } = string.Empty;
        public Guid? CardOrderId { get; set; }
    }
}
