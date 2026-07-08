using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }

        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? WebsiteUrl { get; set; }

        public ICollection<ProfileLink> CustomLinks { get; set; } = new List<ProfileLink>();

        public Guid? CardTemplateId { get; set; }
        public CardTemplate? CardTemplate { get; set; }

        public ICollection<Card> ActivatedCards { get; set; } = new List<Card>();
    }
}
