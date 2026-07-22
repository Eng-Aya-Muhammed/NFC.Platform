using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Enums;

using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/company/employees")]
    [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
    public class EmployeeController(
        IEmployeeService employeeService) : ControllerBase
    {
        private readonly IEmployeeService _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request, [FromQuery] string? search)
        {
            var result = await _employeeService.GetPagedEmployeesAsync(request, search);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _employeeService.GetEmployeeDetailsAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
        {
            var result = await _employeeService.CreateEmployeeAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return StatusCode(result.StatusCode, result); // Returns 201 Created or custom response format
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateEmployeeRequest request)
        {
            var result = await _employeeService.UpdateEmployeeJobDetailsAsync(id, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _employeeService.SoftDeleteEmployeeAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
