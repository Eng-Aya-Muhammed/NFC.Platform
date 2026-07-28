using System.Threading.Tasks;
using NFC.Platform.BuildingBlocks.Common.Models;

namespace NFC.Platform.BuildingBlocks.Common.Interfaces
{
    public interface IExcelExportService
    {
        byte[] GenerateExcel(ExportDataContainer dataContainer);
    }

    public interface IPdfExportService
    {
        byte[] GeneratePdf(ExportDataContainer dataContainer);
    }
}
