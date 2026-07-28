using System;
using System.IO;
using OfficeOpenXml;
using Xunit;

namespace NFC.Platform.Tests.Architecture
{
    public class GenerateExcelTestFiles
    {
        [Fact]
        public void CreateExcelFilesForTesting()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            string targetDir = @"d:\NFC.Platform";

            // 1. Generate employees_50.xlsx (50 employees)
            string file50Path = Path.Combine(targetDir, "employees_50.xlsx");
            GenerateExcelFile(file50Path, 50);

            // 2. Generate employees_110.xlsx (110 employees)
            string file110Path = Path.Combine(targetDir, "employees_110.xlsx");
            GenerateExcelFile(file110Path, 110);

            Assert.True(File.Exists(file50Path));
            Assert.True(File.Exists(file110Path));
        }

        private static void GenerateExcelFile(string filePath, int totalRows)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Employees");

            // Write Header
            worksheet.Cells[1, 1].Value = "Name";
            worksheet.Cells[1, 2].Value = "Email";
            worksheet.Cells[1, 3].Value = "Phone";
            worksheet.Cells[1, 4].Value = "JobTitle";
            worksheet.Cells[1, 5].Value = "Department";
            worksheet.Cells[1, 6].Value = "RequiresCard";
            worksheet.Cells[1, 7].Value = "NumberOfCards";

            // Write Rows
            for (int i = 1; i <= totalRows; i++)
            {
                int row = i + 1;
                worksheet.Cells[row, 1].Value = $"الموظف تجربة رقم {i}";
                worksheet.Cells[row, 2].Value = $"employee{i:D3}@testcompany.com";
                worksheet.Cells[row, 3].Value = $"+96650{i:D7}";
                worksheet.Cells[row, 4].Value = i % 2 == 0 ? "مهندس برمجيات" : "مدير تسويق";
                worksheet.Cells[row, 5].Value = i % 2 == 0 ? "تكنولوجيا المعلومات" : "التسويق";
                worksheet.Cells[row, 6].Value = "true";
                worksheet.Cells[row, 7].Value = 1;
            }

            package.SaveAs(new FileInfo(filePath));
        }
    }
}
