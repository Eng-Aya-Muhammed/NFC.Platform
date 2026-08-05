using NFC.Platform.Application.DTOs.Employee;

namespace NFC.Platform.Application.Interfaces.Services;

public interface IVCardService
{
    string BuildVCardString(EmployeeDetailsDto dto);

    byte[] BuildVCardBytes(EmployeeDetailsDto dto);
}
