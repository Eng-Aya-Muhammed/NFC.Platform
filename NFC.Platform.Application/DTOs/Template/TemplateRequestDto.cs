using System;

namespace NFC.Platform.Application.DTOs.Template;

    public class TemplateRequestDto
    {
        public Guid Id { get; set; }
        public Guid RequestedByUserId { get; set; }
        public string RequestedByUsername { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ReferenceImageUrl { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

