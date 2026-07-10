namespace NFC.Platform.Application.DTOs
{
    public class CreateTemplateRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ReferenceImageUrl { get; set; }
        public string? Notes { get; set; }
    }
}
