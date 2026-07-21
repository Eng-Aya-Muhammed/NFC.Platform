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

        /// <summary>
        /// Updates profile links using a collection of CustomLinkInput.
        /// </summary>
        public static void UpdateCustomLinks(this UserProfile profile, IEnumerable<CustomLinkInput> links)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var activeLinks = (links ?? Array.Empty<CustomLinkInput>())
                .Where(l => !string.IsNullOrWhiteSpace(l.Title) && !string.IsNullOrWhiteSpace(l.Url))
                .ToList();

            // 1. Remove all old links
            profile.CustomLinks.Clear();

            // 2. Add active links in order
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

        /// <summary>
        /// Updates profile links using a collection of URLs (used by B2C profile synchronization).
        /// </summary>
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

            // 1. Separate existing links vs obsolete
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

            // 2. Remove obsolete links
            foreach (var obsolete in obsoleteLinks)
            {
                profile.CustomLinks.Remove(obsolete);
            }

            // 3. Add or update active links in order
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
                        Title = url, // By default we just use URL as title in B2C sync
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
