using System;
using Microsoft.Extensions.Localization;

namespace NFC.Platform.BuildingBlocks.Localization
{
    public class MessageService(
        IStringLocalizer<SuccessMessages> successLocalizer,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        IStringLocalizer<ValidationMessages> validationLocalizer,
        IStringLocalizer<BusinessMessages> businessLocalizer,
        IStringLocalizer<ExportMessages>? exportLocalizer = null) : IMessageService
    {
        private readonly IStringLocalizer<SuccessMessages> _successLocalizer = successLocalizer ?? throw new ArgumentNullException(nameof(successLocalizer));
        private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = errorLocalizer ?? throw new ArgumentNullException(nameof(errorLocalizer));
        private readonly IStringLocalizer<ValidationMessages> _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
        private readonly IStringLocalizer<BusinessMessages> _businessLocalizer = businessLocalizer ?? throw new ArgumentNullException(nameof(businessLocalizer));
        private readonly IStringLocalizer<ExportMessages>? _exportLocalizer = exportLocalizer;

        public string Get(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            bool hasArgs = args != null && args.Length > 0;

            // 1. Search in Export Messages (if registered)
            if (_exportLocalizer != null)
            {
                var exportResult = hasArgs ? _exportLocalizer[key, args] : _exportLocalizer[key];
                if (exportResult != null && !exportResult.ResourceNotFound)
                {
                    return exportResult.Value;
                }
            }

            // 2. Search in Success Messages
            var successResult = hasArgs ? _successLocalizer[key, args] : _successLocalizer[key];
            if (successResult != null && !successResult.ResourceNotFound)
            {
                return successResult.Value;
            }

            // 3. Search in Error Messages
            var errorResult = hasArgs ? _errorLocalizer[key, args] : _errorLocalizer[key];
            if (errorResult != null && !errorResult.ResourceNotFound)
            {
                return errorResult.Value;
            }

            // 4. Search in Validation Messages
            var validationResult = hasArgs ? _validationLocalizer[key, args] : _validationLocalizer[key];
            if (validationResult != null && !validationResult.ResourceNotFound)
            {
                return validationResult.Value;
            }

            // 5. Search in Business Messages
            var businessResult = hasArgs ? _businessLocalizer[key, args] : _businessLocalizer[key];
            if (businessResult != null && !businessResult.ResourceNotFound)
            {
                return businessResult.Value;
            }

            return hasArgs ? string.Format(key, args) : key;
        }
    }
}
