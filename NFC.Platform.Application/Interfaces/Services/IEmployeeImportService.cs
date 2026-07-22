using System;
using System.Threading.Tasks;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Application.DTOs.CardOrder;

namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service responsible for managing the background job of importing employees from an Excel file.
/// </summary>
public interface IEmployeeImportService
{
    /// <summary>
    /// Executes the employee import background job. Runs in a Hangfire background thread.
    /// </summary>
    Task ProcessImportJobAsync(Guid jobId);

    /// <summary>
    /// Retrieves the Excel ingestion status for a bulk order or import job.
    /// </summary>
    Task<ServiceResult<EmployeesImportStatusDto>> GetImportStatusAsync(Guid orderId);
}
