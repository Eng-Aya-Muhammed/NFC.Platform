using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardDesign;

public class CreateCardDesignRequest
{
    public string? ExcelDataUrl { get; set; }

    public Guid? CardPackageId { get; set; }

    public int? CustomQuantity { get; set; }

    public CardDesignType CardDesignType { get; set; }

    public string? FrontDesignUrl { get; set; }

    public string? BackDesignUrl { get; set; }

    public Guid CardTypeId { get; set; }
    public string? Notes { get; set; }
}
