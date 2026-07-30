namespace NFC.Platform.Application.DTOs.Company;

    public class UpdateCompanyProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string CommercialRegistry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public Domain.Enums.CompanySize? CompanySize { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? IndustryType { get => Activity; set => Activity = value ?? string.Empty; }
        public string? CommercialRegistrationNumber { get => CommercialRegistry; set => CommercialRegistry = value ?? string.Empty; }
        public string? LogoUrl { get; set; }
        public string? Bio { get; set; }
        public string? WebsiteUrl { get; set; }
        public System.Collections.Generic.List<Profile.CustomLinkInput>? Links { get; set; }
    }

