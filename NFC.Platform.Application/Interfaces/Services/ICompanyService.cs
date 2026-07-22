using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Company;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface ICompanyService
    {
        Task<ServiceResult<CompanyProfileDto>> GetMyCompanyProfileAsync();
        Task<ServiceResult<CompanyProfileDto>> UpdateCompanyProfileAsync(UpdateCompanyProfileRequest request);
        Task<ServiceResult> ChangeCompanyAdminPasswordAsync(CompanyChangePasswordRequest request);
        Task<ServiceResult<CompanyDashboardDto>> GetCompanyDashboardAsync();

        /// <summary>
        /// Sets the company's digital profile template.
        /// Available via PATCH /api/company/template.
        /// </summary>
        Task<ServiceResult<CompanyProfileDto>> UpdateCompanyTemplateAsync(Guid? templateId);
    }
}
