using System.Collections.Generic;
using System.Globalization;

namespace NFC.Platform.BuildingBlocks.Common.Models
{
    public enum ExportFormat
    {
        Excel,
        Pdf
    }

    public class ExportColumnHeader
    {
        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class ExportRow
    {
        public Dictionary<string, string> Cells { get; set; } = new();
    }

    public class ExportDataContainer
    {
        public string Title { get; set; } = string.Empty;
        public List<ExportColumnHeader> Headers { get; set; } = new();
        public List<ExportRow> Rows { get; set; } = new();
        public bool IsRtl { get; set; }
        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    }
}
