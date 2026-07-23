using System;
using FluentValidation;

namespace NFC.Platform.Application.Validators
{
    public static class ValidationExtensions
    {
        /// <summary>
        /// Validates that the string is a valid absolute URL.
        /// </summary>
        public static IRuleBuilderOptions<T, string?> MustBeValidUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder.Must(url => 
                string.IsNullOrWhiteSpace(url) || 
                (Uri.TryCreate(url, UriKind.Absolute, out var outUri) && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps)));
        }
    }
}
