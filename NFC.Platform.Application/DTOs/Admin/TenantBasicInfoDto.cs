using System;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class TenantBasicInfoDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TypeOfCompanyActivity { get; set; } = string.Empty;
        public string CompanySize { get; set; } = string.Empty;
        public string CommercialNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public Guid? SelectedTemplateId { get; set; }
        public string? SelectedTemplateName { get; set; }
    }
}
