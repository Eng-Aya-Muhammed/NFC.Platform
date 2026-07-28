using System;
using System.IO;
using ClosedXML.Excel;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;

namespace NFC.Platform.Infrastructure.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public byte[] GenerateExcel(ExportDataContainer dataContainer)
        {
            using var workbook = new XLWorkbook();
            var sheetName = string.IsNullOrWhiteSpace(dataContainer.Title) ? "Export" : dataContainer.Title;
            // Sheet name max length in Excel is 31 chars
            if (sheetName.Length > 30)
            {
                sheetName = sheetName.Substring(0, 30);
            }

            var worksheet = workbook.Worksheets.Add(sheetName);

            if (dataContainer.IsRtl)
            {
                worksheet.RightToLeft = true;
            }

            // 1. Write Header Row
            int colIndex = 1;
            foreach (var header in dataContainer.Headers)
            {
                var cell = worksheet.Cell(1, colIndex);
                cell.Value = header.DisplayName;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B"); // Dark Slate Header
                cell.Style.Alignment.Horizontal = dataContainer.IsRtl ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                colIndex++;
            }

            // 2. Write Data Rows
            int rowIndex = 2;
            foreach (var row in dataContainer.Rows)
            {
                colIndex = 1;
                bool isEvenRow = rowIndex % 2 == 0;

                foreach (var header in dataContainer.Headers)
                {
                    var cell = worksheet.Cell(rowIndex, colIndex);
                    row.Cells.TryGetValue(header.PropertyName, out var cellValue);
                    cell.Value = cellValue ?? string.Empty;

                    cell.Style.Alignment.Horizontal = dataContainer.IsRtl ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    if (isEvenRow)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC"); // Light Zebra Striping
                    }

                    colIndex++;
                }
                rowIndex++;
            }

            // 3. Formatting Features
            if (rowIndex > 2 && dataContainer.Headers.Count > 0)
            {
                var usedRange = worksheet.RangeUsed();
                if (usedRange != null)
                {
                    usedRange.SetAutoFilter();
                }
            }

            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents(10.0, 50.0); // Min width 10, Max width 50

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
