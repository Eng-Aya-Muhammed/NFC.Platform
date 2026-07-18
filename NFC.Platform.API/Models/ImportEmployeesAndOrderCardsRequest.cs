using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.API.Models
{
    public class ImportEmployeesAndOrderCardsRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;
        public CardType CardType { get; set; } = CardType.Plastic;
        public CardDesignType CardDesignType { get; set; } = CardDesignType.BuiltInTemplate;
        public string? Notes { get; set; }
    }
}
