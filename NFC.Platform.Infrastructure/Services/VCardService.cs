using System;
using System.Text;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.Infrastructure.Services;

/// <summary>
/// Infrastructure service for building vCard 3.0 formatted contact cards.
/// Compatible with iOS Apple Contacts, Android Google Contacts, Outlook, and macOS.
/// </summary>
public class VCardService : IVCardService
{
    public string BuildVCardString(EmployeeDetailsDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var sb = new StringBuilder();

        sb.AppendLine("BEGIN:VCARD");
        sb.AppendLine("VERSION:3.0");

        // Formatted Name & Structured Name
        var fullName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName.Trim() : "Contact";
        sb.AppendLine($"FN:{EscapeVCardValue(fullName)}");
        sb.AppendLine($"N:{BuildStructuredName(fullName)}");

        // Title (JobTitle)
        if (!string.IsNullOrWhiteSpace(dto.JobTitle))
        {
            sb.AppendLine($"TITLE:{EscapeVCardValue(dto.JobTitle.Trim())}");
        }

        // Organization & Department
        if (!string.IsNullOrWhiteSpace(dto.CompanyName) || !string.IsNullOrWhiteSpace(dto.Department))
        {
            var company = EscapeVCardValue(dto.CompanyName?.Trim() ?? string.Empty);
            var department = EscapeVCardValue(dto.Department?.Trim() ?? string.Empty);
            sb.AppendLine($"ORG:{company};{department}");
        }

        // Email (ContactEmail or Email fallback)
        var email = !string.IsNullOrWhiteSpace(dto.ContactEmail) ? dto.ContactEmail.Trim() : dto.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            sb.AppendLine($"EMAIL;TYPE=INTERNET,WORK:{EscapeVCardValue(email)}");
        }

        // Cell Phone
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            sb.AppendLine($"TEL;TYPE=CELL,VOICE:{EscapeVCardValue(dto.Phone.Trim())}");
        }

        // WhatsApp
        if (!string.IsNullOrWhiteSpace(dto.WhatsApp))
        {
            sb.AppendLine($"TEL;TYPE=CELL,WA:{EscapeVCardValue(dto.WhatsApp.Trim())}");
        }

        // Address
        if (!string.IsNullOrWhiteSpace(dto.Address))
        {
            sb.AppendLine($"ADR;TYPE=WORK:;;{EscapeVCardValue(dto.Address.Trim())};;;;");
        }

        // Bio Note
        if (!string.IsNullOrWhiteSpace(dto.Bio))
        {
            sb.AppendLine($"NOTE:{EscapeVCardValue(dto.Bio.Trim())}");
        }

        // Profile URL
        if (!string.IsNullOrWhiteSpace(dto.ProfileUrl))
        {
            sb.AppendLine($"URL:{EscapeVCardValue(dto.ProfileUrl.Trim())}");
        }

        // Profile Picture URL
        if (!string.IsNullOrWhiteSpace(dto.ProfilePictureUrl))
        {
            sb.AppendLine($"PHOTO;VALUE=URI:{EscapeVCardValue(dto.ProfilePictureUrl.Trim())}");
        }

        // Custom Links / Social Profiles
        if (dto.Links != null)
        {
            foreach (var link in dto.Links)
            {
                if (!string.IsNullOrWhiteSpace(link.Url))
                {
                    var label = !string.IsNullOrWhiteSpace(link.Title) ? link.Title.Trim().ToLowerInvariant() : "website";
                    sb.AppendLine($"X-SOCIALPROFILE;TYPE={label}:{EscapeVCardValue(link.Url.Trim())}");
                }
            }
        }

        sb.AppendLine("END:VCARD");

        return sb.ToString();
    }

    public byte[] BuildVCardBytes(EmployeeDetailsDto dto)
    {
        var vcardString = BuildVCardString(dto);
        return Encoding.UTF8.GetBytes(vcardString);
    }

    private static string BuildStructuredName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return $"{EscapeVCardValue(parts[0])};;;;";
        }
        if (parts.Length >= 2)
        {
            var firstName = EscapeVCardValue(parts[0]);
            var lastName = EscapeVCardValue(parts[^1]);
            var middle = parts.Length > 2 ? EscapeVCardValue(string.Join(' ', parts[1..^1])) : string.Empty;
            return $"{lastName};{firstName};{middle};;";
        }
        return ";;;;";
    }

    private static string EscapeVCardValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }
}
