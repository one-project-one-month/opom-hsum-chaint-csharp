using HsumChaint.API.Authorization;
using HsumChaint.Domain;
using HsumChaint.Domain.Features.RolePermission.DTOs;
using HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;
using HsumChaint.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Features.RolePermission.Controllers;

[ApiController]
[Route("api/v1/roles")]
public class RolePermissionController(IRolePermissionService service) : ControllerBase
{
    [HttpGet, HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRoles() => Result(await service.GetRoles());

    [HttpGet("{id:int}"), HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRole(int id) => Result(await service.GetRoleById(id));

    [HttpPost, HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> CreateRole(CreateRoleRequestDto request) => Result(await service.CreateRole(request));

    [HttpPut("{id:int}"), HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> UpdateRole(int id, UpdateRoleRequestDto request) => Result(await service.UpdateRole(id, request));

    [HttpDelete("{id:int}"), HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> DeleteRole(int id) => Result(await service.DeleteRole(id));

    [HttpGet("/api/v1/permissions"), HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetPermissions() => Result(await service.GetPermissions());

    [HttpPost("assign-permissions"), HasPermission(Permissions.Roles.Assign)]
    public async Task<IActionResult> AssignPermissions(AssignRolePermissionsRequestDto request) => Result(await service.AssignPermissions(request));

    private IActionResult Result<T>(ApplicationCommonResponseModel<T> response) => response.IsSuccess == true
        ? Ok(response)
        : response.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? NotFound(response) : BadRequest(response);
}
