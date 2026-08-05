using System;
using System.Collections.Generic;
using System.Linq;
using NFC.Platform.Application.Constants;
using NFC.Platform.Application.DTOs.Profile;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Extensions
{
    public static class UserProfileExtensions
    {
        private static readonly string[] LineSeparators = ["\r\n", "\n"];

        public static void UpdateCustomLinks(this UserProfile profile, IEnumerable<CustomLinkInput> links)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var activeLinks = (links ?? Array.Empty<CustomLinkInput>())
                .Where(l => !string.IsNullOrWhiteSpace(l.Title) && !string.IsNullOrWhiteSpace(l.Url))
                .ToList();

            profile.CustomLinks.Clear();

            var displayOrder = 1;
            foreach (var link in activeLinks)
            {
                profile.CustomLinks.Add(new ProfileLink
                {
                    Id = Guid.Empty,
                    Title = link.Title,
                    Url = link.Url,
                    DisplayOrder = displayOrder++,
                    TenantId = profile.TenantId,
                    UserProfileId = profile.Id
                });
            }
        }

        public static void UpdateCustomLinks(this UserProfile profile, IEnumerable<string> links)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var activeLinks = (links ?? Array.Empty<string>())
                .Select(url => url.Trim())
                .Where(url => !string.IsNullOrEmpty(url))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var newUrlsSet = new HashSet<string>(activeLinks, StringComparer.OrdinalIgnoreCase);

            var existingLinksLookup = new Dictionary<string, ProfileLink>(StringComparer.OrdinalIgnoreCase);
            var obsoleteLinks = new List<ProfileLink>();

            foreach (var link in profile.CustomLinks)
            {
                if (newUrlsSet.Contains(link.Url))
                {
                    existingLinksLookup[link.Url] = link;
                }
                else
                {
                    obsoleteLinks.Add(link);
                }
            }

            foreach (var obsolete in obsoleteLinks)
            {
                profile.CustomLinks.Remove(obsolete);
            }

            var displayOrder = 1;
            foreach (var url in activeLinks)
            {
                if (existingLinksLookup.TryGetValue(url, out var existing))
                {
                    existing.DisplayOrder = displayOrder++;
                }
                else
                {
                    profile.CustomLinks.Add(new ProfileLink
                    {
                        Id = Guid.Empty,
                        Title = url,
                        Url = url,
                        DisplayOrder = displayOrder++,
                        TenantId = profile.TenantId,
                        UserProfileId = profile.Id
                    });
                }
            }
        }
    }
}
