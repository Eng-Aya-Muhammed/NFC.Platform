namespace NFC.Platform.Application.DTOs.Employee;

    public class UpdateMyProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }

        public System.Collections.Generic.List<Profile.CustomLinkInput> Links { get; set; } = [];

        public string? Subdomain { get; set; }
    }

