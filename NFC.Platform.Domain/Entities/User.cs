using System;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Domain.Entities
{
    public class User : BaseEntity, ITenantEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpires { get; set; }

        public AccountType AccountType { get; set; } = AccountType.Individual;
        public UserStatus Status { get; set; } = UserStatus.Active;
        
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; }
        public UserProfile? UserProfile { get; set; }
    }
}
