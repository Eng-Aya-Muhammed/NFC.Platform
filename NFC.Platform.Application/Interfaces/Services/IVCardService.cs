using NFC.Platform.Application.DTOs.Employee;

namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service contract for generating vCard (.vcf) formatted contact cards.
/// Produces vCard 3.0 specification compliant outputs.
/// </summary>
public interface IVCardService
{
    /// <summary>
    /// Generates a vCard 3.0 formatted UTF-8 string for the provided profile details.
    /// </summary>
    string BuildVCardString(EmployeeDetailsDto dto);

    /// <summary>
    /// Generates a vCard 3.0 UTF-8 encoded byte array ready for HTTP file streaming.
    /// </summary>
    byte[] BuildVCardBytes(EmployeeDetailsDto dto);
}
