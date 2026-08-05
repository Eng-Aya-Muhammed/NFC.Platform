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

        public string Get(string key, params object[]? args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var safeArgs = args ?? Array.Empty<object>();
            bool hasArgs = safeArgs.Length > 0;

            if (_exportLocalizer != null)
            {
                var exportResult = hasArgs ? _exportLocalizer[key, safeArgs] : _exportLocalizer[key];
                if (exportResult != null && !exportResult.ResourceNotFound)
                {
                    return exportResult.Value;
                }
            }

            var successResult = hasArgs ? _successLocalizer[key, safeArgs] : _successLocalizer[key];
            if (successResult != null && !successResult.ResourceNotFound)
            {
                return successResult.Value;
            }

            var errorResult = hasArgs ? _errorLocalizer[key, safeArgs] : _errorLocalizer[key];
            if (errorResult != null && !errorResult.ResourceNotFound)
            {
                return errorResult.Value;
            }

            var validationResult = hasArgs ? _validationLocalizer[key, safeArgs] : _validationLocalizer[key];
            if (validationResult != null && !validationResult.ResourceNotFound)
            {
                return validationResult.Value;
            }

            var businessResult = hasArgs ? _businessLocalizer[key, safeArgs] : _businessLocalizer[key];
            if (businessResult != null && !businessResult.ResourceNotFound)
            {
                return businessResult.Value;
            }

            return hasArgs ? string.Format(key, safeArgs) : key;
        }
    }
}
