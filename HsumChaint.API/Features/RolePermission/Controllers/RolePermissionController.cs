using HsumChaint.API.Extensions;
using HsumChaint.Shared;
using HsumChaint.API.Authorization;
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
    public async Task<IActionResult> GetRoles([FromQuery] PaginationRequest pagination) => (await service.GetRoles(pagination)).ToActionResult();

    [HttpGet("{id:int}"), HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRole(int id) => (await service.GetRoleById(id)).ToActionResult();

    [HttpPost, HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> CreateRole(CreateRoleRequestDto request) => (await service.CreateRole(request)).ToActionResult();

    [HttpPut("{id:int}"), HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> UpdateRole(int id, UpdateRoleRequestDto request) => (await service.UpdateRole(id, request)).ToActionResult();

    [HttpDelete("{id:int}"), HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> DeleteRole(int id) => (await service.DeleteRole(id)).ToActionResult();

    [HttpGet("/api/v1/permissions"), HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetPermissions([FromQuery] PaginationRequest pagination) => (await service.GetPermissions(pagination)).ToActionResult();

    [HttpPost("assign-permissions"), HasPermission(Permissions.Roles.Assign)]
    public async Task<IActionResult> AssignPermissions(AssignRolePermissionsRequestDto request) => (await service.AssignPermissions(request)).ToActionResult();

}
