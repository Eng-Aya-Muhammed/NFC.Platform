using System;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.Infrastructure.Services
{
    public class ExcelParser : IExcelParser
    {
        public List<ExcelEmployeeImportDto> ParseEmployeesFromExcel(Stream excelStream)
        {
            if (excelStream == null)
                throw new ArgumentNullException(nameof(excelStream));

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var rows = new List<ExcelEmployeeImportDto>();
            using var reader = ExcelReaderFactory.CreateReader(excelStream);

            var nameCol = -1;
            var emailCol = -1;
            var phoneCol = -1;
            var jobTitleCol = -1;
            var departmentCol = -1;

            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var headerVal = reader.GetValue(i)?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(headerVal)) continue;

                    if (headerVal.Contains("name", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("الاسم", StringComparison.OrdinalIgnoreCase))
                        nameCol = i;
                    else if (headerVal.Contains("email", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("البريد", StringComparison.OrdinalIgnoreCase))
                        emailCol = i;
                    else if (headerVal.Contains("phone", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("الهاتف", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("جوال", StringComparison.OrdinalIgnoreCase))
                        phoneCol = i;
                    else if (headerVal.Contains("title", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("وظيفة", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("المسمى", StringComparison.OrdinalIgnoreCase))
                        jobTitleCol = i;
                    else if (headerVal.Contains("department", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("قسم", StringComparison.OrdinalIgnoreCase))
                        departmentCol = i;
                }
            }

            if (nameCol == -1) nameCol = 0;
            if (emailCol == -1) emailCol = 1;
            if (phoneCol == -1) phoneCol = 2;
            if (jobTitleCol == -1) jobTitleCol = 3;
            if (departmentCol == -1) departmentCol = 4;

            while (reader.Read())
            {
                var name = nameCol < reader.FieldCount ? reader.GetValue(nameCol)?.ToString()?.Trim() : null;
                var email = emailCol < reader.FieldCount ? reader.GetValue(emailCol)?.ToString()?.Trim() : null;

                var phone = phoneCol < reader.FieldCount ? reader.GetValue(phoneCol)?.ToString()?.Trim() : null;
                var jobTitle = jobTitleCol < reader.FieldCount ? reader.GetValue(jobTitleCol)?.ToString()?.Trim() : null;
                var department = departmentCol < reader.FieldCount ? reader.GetValue(departmentCol)?.ToString()?.Trim() : null;

                bool isCompletelyEmptyRow = string.IsNullOrWhiteSpace(name) && 
                                            string.IsNullOrWhiteSpace(email) &&
                                            string.IsNullOrWhiteSpace(phone) &&
                                            string.IsNullOrWhiteSpace(jobTitle) &&
                                            string.IsNullOrWhiteSpace(department);

                if (isCompletelyEmptyRow) 
                {
                    continue; // Skip only if the entire row is completely blank
                }

                rows.Add(new ExcelEmployeeImportDto
                {
                    Name = name,
                    Email = email,
                    Phone = phone,
                    JobTitle = jobTitle,
                    Department = department
                });
            }

            return rows;
        }
    }
}
