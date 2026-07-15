using System.Collections.Generic;
using System.IO;
using NFC.Platform.Application.DTOs.CardOrder;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IExcelParser
    {
        List<ExcelEmployeeImportDto> ParseEmployeesFromExcel(Stream excelStream);
    }
}
