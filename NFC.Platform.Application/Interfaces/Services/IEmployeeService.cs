using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<ServiceResult<PagedResult<EmployeeDto>>> GetPagedEmployeesAsync(PaginationRequest request, string? search);
        Task<ServiceResult<EmployeeDetailsDto>> GetEmployeeDetailsAsync(Guid id);
        Task<ServiceResult<EmployeeDetailsDto>> CreateEmployeeAsync(CreateEmployeeRequest request);
        Task<ServiceResult<EmployeeDetailsDto>> UpdateEmployeeJobDetailsAsync(Guid id, UpdateEmployeeRequest request);
        Task<ServiceResult> SoftDeleteEmployeeAsync(Guid id);

        Task<ServiceResult<byte[]>> ExportEmployeesAsync(ExportFormat format, string? search);

        Task<ServiceResult<List<Guid>>> UpsertEmployeesFromExcelAsync(string excelUrl, Guid companyId, Guid tenantId);
    }
}
