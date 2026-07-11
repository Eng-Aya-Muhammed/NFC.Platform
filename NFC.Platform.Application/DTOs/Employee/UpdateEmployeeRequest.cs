using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Employee;

    public class UpdateEmployeeRequest
    {
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public UserStatus Status { get; set; }
    }

