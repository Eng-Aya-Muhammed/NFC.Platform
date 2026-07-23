using System;
using Microsoft.Extensions.Localization;

namespace NFC.Platform.BuildingBlocks.Localization
{
    /// <summary>
    /// A thin wrapper over the standard .NET <see cref="IStringLocalizer{T}"/> that resolves the Generic Type constraint problem.
    /// <para>
    /// <see cref="IStringLocalizer{T}"/> requires a concrete Generic Type linked to a specific class at injection time,
    /// which is impractical when the same localization must be injected into many general-purpose classes
    /// (such as Validators, Services, and Middleware) without coupling them all to the same anchor Type.
    /// </para>
    /// <para>
    /// <see cref="IMessageService"/> solves this by fixing the anchor Types internally (one per resource category)
    /// and exposing a single, Type-independent interface to the rest of the system.
    /// </para>
    /// <para>
    /// All formatting and culture resolution is delegated entirely to the underlying <see cref="IStringLocalizer{T}"/> instances;
    /// this class performs no file reading or string formatting of its own.
    /// </para>
    /// </summary>
    public class MessageService(
        IStringLocalizer<SuccessMessages> successLocalizer,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        IStringLocalizer<ValidationMessages> validationLocalizer,
        IStringLocalizer<BusinessMessages> businessLocalizer) : IMessageService
    {
        private readonly IStringLocalizer<SuccessMessages> _successLocalizer = successLocalizer ?? throw new ArgumentNullException(nameof(successLocalizer));
        private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = errorLocalizer ?? throw new ArgumentNullException(nameof(errorLocalizer));
        private readonly IStringLocalizer<ValidationMessages> _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
        private readonly IStringLocalizer<BusinessMessages> _businessLocalizer = businessLocalizer ?? throw new ArgumentNullException(nameof(businessLocalizer));

        public string Get(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            bool hasArgs = args != null && args.Length > 0;

            // 1. Search in Success Messages
            var successResult = hasArgs ? _successLocalizer[key, args] : _successLocalizer[key];
            if (successResult != null && !successResult.ResourceNotFound)
            {
                return successResult.Value;
            }

            // 2. Search in Error Messages
            var errorResult = hasArgs ? _errorLocalizer[key, args] : _errorLocalizer[key];
            if (errorResult != null && !errorResult.ResourceNotFound)
            {
                return errorResult.Value;
            }

            // 3. Search in Validation Messages
            var validationResult = hasArgs ? _validationLocalizer[key, args] : _validationLocalizer[key];
            if (validationResult != null && !validationResult.ResourceNotFound)
            {
                return validationResult.Value;
            }

            // 4. Search in Business Messages
            var businessResult = hasArgs ? _businessLocalizer[key, args] : _businessLocalizer[key];
            if (businessResult != null && !businessResult.ResourceNotFound)
            {
                return businessResult.Value;
            }

            return hasArgs ? string.Format(key, args) : key;
        }
    }
}
