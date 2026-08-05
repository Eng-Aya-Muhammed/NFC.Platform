using System;
using FluentValidation;

namespace NFC.Platform.Application.Validators
{
    public static class ValidationExtensions
    {
        public static IRuleBuilderOptions<T, string?> MustBeValidUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder.Must(url =>
                string.IsNullOrWhiteSpace(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var outUri) && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps)));
        }

        public static IRuleBuilderOptions<T, string?> MustBeValidPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder.Must(phone => string.IsNullOrWhiteSpace(phone) || System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+?[1-9]\d{7,14}$"));
        }
    }
}
