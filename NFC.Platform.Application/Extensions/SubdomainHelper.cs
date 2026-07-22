

namespace NFC.Platform.Application.Extensions
{
    /// <summary>
    /// Utility helpers for generating and validating URL-safe subdomains.
    /// </summary>
    public static class SubdomainHelper
    {
        /// <summary>
        /// Converts any string into a lowercase, URL-safe slug containing
        /// only letters, digits, and hyphens.
        /// </summary>
        public static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "user";

            var normalized = input.Trim().ToLowerInvariant().Replace(" ", "-");
            
            // Remove consecutive hyphens
            while (normalized.Contains("--"))
            {
                normalized = normalized.Replace("--", "-");
            }

            return new string(
                normalized.Where(c => char.IsLetterOrDigit(c) || c == '-')
                          .ToArray()).Trim('-');
        }

        /// <summary>
        /// Generates a unique subdomain slug against an in-memory set.
        /// Automatically adds the chosen slug to <paramref name="existingSlugs"/>
        /// so that subsequent calls in the same batch cannot produce duplicates.
        /// Use this overload in bulk operations (e.g. Excel import) to avoid
        /// N+1 database queries.
        /// </summary>
        /// <param name="fullName">The employee/user full name to derive the slug from.</param>
        /// <param name="existingSlugs">
        ///   A <see cref="HashSet{T}"/> pre-loaded with every subdomain already
        ///   persisted in the database (case-insensitive). Will be mutated.
        /// </param>
        public static string GenerateUnique(string fullName, HashSet<string> existingSlugs)
        {
            var baseSlug = Slugify(fullName);
            var candidate = baseSlug;
            var counter = 1;

            while (existingSlugs.Contains(candidate))
            {
                candidate = $"{baseSlug}-{counter++}";
            }

            existingSlugs.Add(candidate);
            return candidate;
        }

        /// <summary>
        /// Generates a unique subdomain slug by querying the database.
        /// Use this overload for single-row operations (Create/Update employee,
        /// Update user profile) where loading all slugs upfront is not warranted.
        /// </summary>
        /// <param name="fullName">The employee/user full name to derive the slug from.</param>
        /// <param name="repo">The <see cref="UserProfile"/> repository.</param>
        /// <param name="excludeProfileId">
        ///   When updating an existing profile, pass its ID so the uniqueness
        ///   check does not reject the profile's own current subdomain.
        /// </param>
        public static async Task<string> GenerateUniqueAsync(
            string fullName,
            IGenericRepository<UserProfile> repo,
            Guid? excludeProfileId = null)
        {
            var baseSlug = Slugify(fullName);
            var candidate = baseSlug;
            var counter = 1;

            while (await repo.GetQueryable()
                             .IgnoreQueryFilters()
                             .AnyAsync(p => p.Subdomain == candidate
                                         && (excludeProfileId == null || p.Id != excludeProfileId)))
            {
                candidate = $"{baseSlug}-{counter++}";
            }

            return candidate;
        }
    }
}
