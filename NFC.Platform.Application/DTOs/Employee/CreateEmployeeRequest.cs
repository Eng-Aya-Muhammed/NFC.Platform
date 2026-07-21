using System;

namespace NFC.Platform.Application.DTOs.Employee;

    public class CreateEmployeeRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }

        public string? ProfilePictureUrl { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }
        public List<Profile.CustomLinkInput> Links { get; set; } = [];
    }

