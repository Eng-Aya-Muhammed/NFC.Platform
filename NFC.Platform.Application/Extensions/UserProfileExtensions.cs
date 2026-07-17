using System;
using System.Collections.Generic;
using System.Linq;
using NFC.Platform.Application.Constants;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Extensions
{
    public static class UserProfileExtensions
    {
        private static readonly string[] LineSeparators = ["\r\n", "\n"];

        /// <summary>
        /// Updates profile custom links using a newline-separated string (used by B2B employee updates).
        /// </summary>
        public static void UpdateCustomLinks(this UserProfile profile, string? customLinksText)
        {
            var uniqueNewUrls = string.IsNullOrWhiteSpace(customLinksText)
                ? Array.Empty<string>()
                : customLinksText.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);

            profile.UpdateCustomLinks(uniqueNewUrls);
        }

        /// <summary>
        /// Updates profile custom links using a collection of URLs (used by B2C profile synchronization).
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

            var standardPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PlatformConstants.LinkedIn,
                PlatformConstants.Facebook,
                PlatformConstants.Instagram,
                PlatformConstants.Website
            };

            // 1. Separate standard links from custom links and classify them
            foreach (var link in profile.CustomLinks)
            {
                if (standardPlatforms.Contains(link.Title))
                    continue;

                if (newUrlsSet.Contains(link.Url))
                {
                    existingLinksLookup[link.Url] = link;
                }
                else
                {
                    obsoleteLinks.Add(link);
                }
            }

            // 2. Remove obsolete custom links
            foreach (var obsolete in obsoleteLinks)
            {
                profile.CustomLinks.Remove(obsolete);
            }

            // 3. Add or update active custom links in order
            var displayOrder = 1;
            foreach (var url in activeLinks)
            {
                if (standardPlatforms.Contains(url))
                    continue;

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
