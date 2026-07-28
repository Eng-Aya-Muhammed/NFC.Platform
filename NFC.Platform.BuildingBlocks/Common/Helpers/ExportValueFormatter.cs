using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public class ExportValueFormatter(IMessageService messageService) : IExportValueFormatter
    {
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        public string Format(object? value, CultureInfo? culture = null)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var currentCulture = culture ?? CultureInfo.CurrentCulture;

            switch (value)
            {
                case bool boolValue:
                    return _messageService.Get(boolValue ? "Export_Bool_Yes" : "Export_Bool_No");

                case Enum enumValue:
                    var enumKey = $"Export_Enum_{enumValue.GetType().Name}_{enumValue}";
                    var localizedEnum = _messageService.Get(enumKey);
                    return string.Equals(localizedEnum, enumKey, StringComparison.Ordinal) ? enumValue.ToString() : localizedEnum;

                case DateTime dateTimeValue:
                    return dateTimeValue.ToString("dd/MM/yyyy HH:mm", currentCulture);

                case DateTimeOffset dateTimeOffsetValue:
                    return dateTimeOffsetValue.ToString("dd/MM/yyyy HH:mm", currentCulture);

#if NET6_0_OR_GREATER
                case DateOnly dateOnlyValue:
                    return dateOnlyValue.ToString("dd/MM/yyyy", currentCulture);

                case TimeOnly timeOnlyValue:
                    return timeOnlyValue.ToString("HH:mm", currentCulture);
#endif

                case decimal decimalValue:
                    return decimalValue.ToString("N2", currentCulture);

                case double doubleValue:
                    return doubleValue.ToString("N2", currentCulture);

                case float floatValue:
                    return floatValue.ToString("N2", currentCulture);

                case Guid guidValue:
                    return guidValue.ToString();

                case IEnumerable<string> stringEnumerable:
                    return string.Join(", ", stringEnumerable);

                case IEnumerable enumerable when value is not string:
                    var items = enumerable.Cast<object>().Select(item => Format(item, currentCulture));
                    return string.Join(", ", items);

                default:
                    return value.ToString() ?? string.Empty;
            }
        }
    }
}
