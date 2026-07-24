using System.Collections.Generic;
using System.IO;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.DTOs.Employee;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IExcelParser
    {
        List<ExcelEmployeeImportDto> ParseEmployeesFromExcel(Stream excelStream);
    }
}
