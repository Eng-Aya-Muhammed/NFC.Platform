using System;

namespace NFC.Platform.Application.DTOs
{
    public class CreateEmployeeRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
    }
}
