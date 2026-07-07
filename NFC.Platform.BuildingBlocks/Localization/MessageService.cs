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
    public class MessageService : IMessageService
    {
        private readonly IStringLocalizer<SuccessMessages> _successLocalizer;
        private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
        private readonly IStringLocalizer<ValidationMessages> _validationLocalizer;
        private readonly IStringLocalizer<BusinessMessages> _businessLocalizer;

        public MessageService(
            IStringLocalizer<SuccessMessages> successLocalizer,
            IStringLocalizer<ErrorMessages> errorLocalizer,
            IStringLocalizer<ValidationMessages> validationLocalizer,
            IStringLocalizer<BusinessMessages> businessLocalizer)
        {
            _successLocalizer = successLocalizer ?? throw new ArgumentNullException(nameof(successLocalizer));
            _errorLocalizer = errorLocalizer ?? throw new ArgumentNullException(nameof(errorLocalizer));
            _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
            _businessLocalizer = businessLocalizer ?? throw new ArgumentNullException(nameof(businessLocalizer));
        }


        public string Get(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            // 1. Search in Success Messages
            var successResult = _successLocalizer[key, args];
            if (!successResult.ResourceNotFound)
            {
                return successResult.Value;
            }

            // 2. Search in Error Messages
            var errorResult = _errorLocalizer[key, args];
            if (!errorResult.ResourceNotFound)
            {
                return errorResult.Value;
            }

            // 3. Search in Validation Messages
            var validationResult = _validationLocalizer[key, args];
            if (!validationResult.ResourceNotFound)
            {
                return validationResult.Value;
            }

            // 4. Search in Business Messages
            var businessResult = _businessLocalizer[key, args];
            if (!businessResult.ResourceNotFound)
            {
                return businessResult.Value;
            }

            return args != null && args.Length > 0 ? string.Format(key, args) : key;
        }
    }
}
