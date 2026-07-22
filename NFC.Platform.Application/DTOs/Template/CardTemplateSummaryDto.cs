using System;

namespace NFC.Platform.Application.DTOs.Template
{
    /// <summary>
    /// Lightweight template projection for embedding inside subscription plan responses.
    /// </summary>
    public class CardTemplateSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
    }
}
