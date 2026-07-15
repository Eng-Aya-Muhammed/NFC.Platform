using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class ResolveTemplateRequestDto
    {
        public TemplateRequestStatus Status { get; set; }
        public string? StyleConfigJson { get; set; }
        public string? Notes { get; set; }
    }
}
