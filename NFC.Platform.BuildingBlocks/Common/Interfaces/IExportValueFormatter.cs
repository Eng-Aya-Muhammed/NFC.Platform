using System.Globalization;

namespace NFC.Platform.BuildingBlocks.Common.Interfaces
{
    public interface IExportValueFormatter
    {
        string Format(object? value, CultureInfo? culture = null);
    }
}
