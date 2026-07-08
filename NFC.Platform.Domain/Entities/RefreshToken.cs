using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresOn { get; set; }
        public bool IsRevoked { get; set; }

        public Guid TenantId { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
