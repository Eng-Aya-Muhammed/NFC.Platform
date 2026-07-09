using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/employee/profile")]
    [Authorize(Policy = AppPolicies.EmployeeOnly)]
    public class EmployeeProfileController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ICurrentTenant _currentTenant;

        public EmployeeProfileController(IEmployeeService employeeService, ICurrentTenant currentTenant)
        {
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
                return Unauthorized(ServiceResult.Unauthorized("User is not authenticated."));

            var result = await _employeeService.GetMyProfileAsync(userId.Value);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateMyProfileRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
                return Unauthorized(ServiceResult.Unauthorized("User is not authenticated."));

            var result = await _employeeService.UpdateMyProfileAsync(userId.Value, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
