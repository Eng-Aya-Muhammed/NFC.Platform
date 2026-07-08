using System;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Domain.Entities
{
    public class ProfileMetric : BaseEntity
    {
        public Guid UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; } = null!;

        public InteractionType InteractionType { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public Guid? ProfileLinkId { get; set; }
        public ProfileLink? ProfileLink { get; set; }
    }
}
