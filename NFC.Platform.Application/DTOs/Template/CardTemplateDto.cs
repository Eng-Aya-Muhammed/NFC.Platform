using System;

namespace NFC.Platform.Application.DTOs.Template;

    public class CardTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PreviewImageUrl { get; set; } = string.Empty;
        public string StyleConfigJson { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

