using System;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.Profile;
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
            var whatsappCol = -1;

            // Store column indices and their title for custom & social link headers
            var linkColumns = new Dictionary<int, string>();

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
                    else if (headerVal.Contains("whatsapp", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("واتساب", StringComparison.OrdinalIgnoreCase) || headerVal.Contains("واتس", StringComparison.OrdinalIgnoreCase))
                        whatsappCol = i;
                    else if (IsLinkHeader(headerVal))
                    {
                        linkColumns[i] = CleanHeaderTitle(headerVal);
                    }
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
                var whatsapp = whatsappCol != -1 && whatsappCol < reader.FieldCount ? reader.GetValue(whatsappCol)?.ToString()?.Trim() : null;

                var customLinks = new List<CustomLinkInput>();

                foreach (var kvp in linkColumns)
                {
                    var colIdx = kvp.Key;
                    var title = kvp.Value;
                    if (colIdx < reader.FieldCount)
                    {
                        var linkUrl = reader.GetValue(colIdx)?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(linkUrl))
                        {
                            customLinks.Add(new CustomLinkInput
                            {
                                Title = title,
                                Url = linkUrl
                            });
                        }
                    }
                }

                bool isCompletelyEmptyRow = string.IsNullOrWhiteSpace(name) && 
                                             string.IsNullOrWhiteSpace(email) &&
                                             string.IsNullOrWhiteSpace(phone) &&
                                             string.IsNullOrWhiteSpace(jobTitle) &&
                                             string.IsNullOrWhiteSpace(department) &&
                                             string.IsNullOrWhiteSpace(whatsapp) &&
                                             customLinks.Count == 0;

                if (isCompletelyEmptyRow) 
                {
                    continue; // Skip completely blank row
                }

                rows.Add(new ExcelEmployeeImportDto
                {
                    Name = name ?? string.Empty,
                    Email = email ?? string.Empty,
                    Phone = phone,
                    JobTitle = jobTitle,
                    Department = department,
                    WhatsApp = whatsapp,
                    CustomLinks = customLinks
                });
            }

            return rows;
        }

        private static bool IsLinkHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return false;

            return header.Contains("facebook", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("فيسبوك", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("instagram", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("انستج", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("انستغ", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("linkedin", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("لينكد", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("website", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("موقع", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("twitter", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("تويتر", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("link", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("url", StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("رابط", StringComparison.OrdinalIgnoreCase);
        }

        private static string CleanHeaderTitle(string header)
        {
            var trimmed = header.Trim();
            if (trimmed.Contains("facebook", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("فيسبوك", StringComparison.OrdinalIgnoreCase))
                return "Facebook";
            if (trimmed.Contains("instagram", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("انستج", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("انستغ", StringComparison.OrdinalIgnoreCase))
                return "Instagram";
            if (trimmed.Contains("linkedin", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("لينكد", StringComparison.OrdinalIgnoreCase))
                return "LinkedIn";
            if (trimmed.Contains("website", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("موقع", StringComparison.OrdinalIgnoreCase))
                return "Website";
            if (trimmed.Contains("twitter", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("تويتر", StringComparison.OrdinalIgnoreCase))
                return "Twitter";

            return trimmed;
        }
    }
}
