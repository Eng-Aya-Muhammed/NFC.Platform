using System;

namespace NFC.Platform.Application.DTOs.Card;

    public class CreateCardRequest
    {
        public string ActivationCode { get; set; } = string.Empty;
        public Guid? CardOrderId { get; set; }
    }

