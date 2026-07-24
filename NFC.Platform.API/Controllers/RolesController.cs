using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs.Role;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/company/roles")]
    [Authorize]
    public class RolesController(IRoleService roleService) : ControllerBase
    {
        private readonly IRoleService _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));

        [HttpGet]
        [HasPermission(AppPermissions.Roles.View)]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _roleService.GetRolesAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [HasPermission(AppPermissions.Roles.View)]
        public async Task<IActionResult> GetRoleById([FromRoute] Guid id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpPost]
        [HasPermission(AppPermissions.Roles.Create)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var result = await _roleService.CreateRoleAsync(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);
            return StatusCode(201, result);
        }

        [HttpPut("{id:guid}/permissions")]
        [HasPermission(AppPermissions.Roles.Update)]
        public async Task<IActionResult> UpdateRolePermissions([FromRoute] Guid id, [FromBody] AssignPermissionsRequest request)
        {
            var result = await _roleService.UpdateRolePermissionsAsync(id, request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [HasPermission(AppPermissions.Roles.Delete)]
        public async Task<IActionResult> DeleteRole([FromRoute] Guid id)
        {
            var result = await _roleService.DeleteRoleAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpPost("{roleId:guid}/users/{userId:guid}")]
        [HasPermission(AppPermissions.Roles.AssignToUser)]
        public async Task<IActionResult> AssignRoleToUser([FromRoute] Guid roleId, [FromRoute] Guid userId)
        {
            var result = await _roleService.AssignRoleToUserAsync(userId, roleId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpDelete("{roleId:guid}/users/{userId:guid}")]
        [HasPermission(AppPermissions.Roles.AssignToUser)]
        public async Task<IActionResult> RevokeRoleFromUser([FromRoute] Guid roleId, [FromRoute] Guid userId)
        {
            var result = await _roleService.RevokeRoleFromUserAsync(userId, roleId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpGet("permissions/available")]
        [HasPermission(AppPermissions.Roles.View)]
        public async Task<IActionResult> GetAvailablePermissions()
        {
            var result = await _roleService.GetAvailablePermissionsAsync();
            return Ok(result);
        }
    }
}
