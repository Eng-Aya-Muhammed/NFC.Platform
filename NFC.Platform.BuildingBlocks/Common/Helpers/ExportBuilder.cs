using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NFC.Platform.BuildingBlocks.Common.Attributes;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public class ExportBuilder(
        IMessageService messageService,
        IExportValueFormatter valueFormatter)
    {
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly IExportValueFormatter _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));

        public ExportDataContainer BuildContainer<T>(IEnumerable<T> data, string titleResourceKey, CultureInfo? culture = null)
        {
            var currentCulture = culture ?? CultureInfo.CurrentCulture;
            var isRtl = string.Equals(currentCulture.TwoLetterISOLanguageName, "ar", StringComparison.OrdinalIgnoreCase);

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<ExportColumnAttribute>()
                })
                .Where(x => x.Attribute != null)
                .OrderBy(x => x.Attribute!.Order)
                .ToList();

            var headers = new List<ExportColumnHeader>
            {
                new ExportColumnHeader
                {
                    PropertyName = "SequenceNumber",
                    DisplayName = _messageService.Get("Export_Col_Sequence"),
                    Order = 0
                }
            };

            headers.AddRange(properties.Select(x => new ExportColumnHeader
            {
                PropertyName = x.Property.Name,
                DisplayName = _messageService.Get(x.Attribute!.ResourceKey),
                Order = x.Attribute.Order
            }));

            var rows = new List<ExportRow>();
            int rowIndex = 1;

            foreach (var item in data)
            {
                if (item == null) continue;

                var row = new ExportRow();
                row.Cells["SequenceNumber"] = rowIndex.ToString(CultureInfo.InvariantCulture);

                foreach (var propInfo in properties)
                {
                    var rawValue = propInfo.Property.GetValue(item);
                    var formattedValue = _valueFormatter.Format(rawValue, currentCulture);
                    row.Cells[propInfo.Property.Name] = formattedValue;
                }
                rows.Add(row);
                rowIndex++;
            }

            var title = _messageService.Get(titleResourceKey);
            if (string.Equals(title, titleResourceKey, StringComparison.Ordinal))
            {
                title = typeof(T).Name.Replace("ExportDto", "").Replace("Dto", "");
            }

            return new ExportDataContainer
            {
                Title = title,
                Headers = headers,
                Rows = rows,
                IsRtl = isRtl,
                Culture = currentCulture
            };
        }
    }
}
