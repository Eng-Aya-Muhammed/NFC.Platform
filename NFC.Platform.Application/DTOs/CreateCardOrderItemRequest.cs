using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Application.DTOs
{
    /// <summary>
    /// Represents a single employee entry within a card order request.
    /// </summary>
    public class CreateCardOrderItemRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string EmployeeName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? JobTitle { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Department { get; set; }

        public Guid? UserProfileId { get; set; }
    }
}
