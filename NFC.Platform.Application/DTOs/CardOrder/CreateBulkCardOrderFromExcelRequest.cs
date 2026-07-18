using System;
using System.IO;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder
{
    public class CreateBulkCardOrderFromExcelRequest
    {
        public Stream ExcelStream { get; set; } = null!;
        public CardType CardType { get; set; } = CardType.Plastic;
        public CardDesignType CardDesignType { get; set; } = CardDesignType.BuiltInTemplate;
        public string? Notes { get; set; }
    }
}
