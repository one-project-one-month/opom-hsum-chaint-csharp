using HsumChaint.API.Authorization;
using HsumChaint.API.Extensions;
using HsumChaint.Domain.Features.RolePermission.DTOs;
using HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;
using HsumChaint.Shared;
using HsumChaint.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Features.RolePermission.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/space-roles")]
public class SpaceRolesController(IRolePermissionService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSpaceRoles([FromQuery] PaginationRequest pagination) =>
        (await service.GetSpaceRoles(pagination)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSpaceRole(int id) =>
        (await service.GetSpaceRole(id)).ToActionResult();

    [HttpPost, HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> CreateSpaceRole(CreateSpaceRoleRequestDto request) =>
        (await service.CreateSpaceRole(request)).ToActionResult();

    [HttpPut("{id:int}"), HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> UpdateSpaceRole(int id, UpdateSpaceRoleRequestDto request)
    {
        request.Id = id;
        return (await service.UpdateSpaceRole(id, request)).ToActionResult();
    }

    [HttpDelete("{id:int}"), HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> DeleteSpaceRole(int id) =>
        (await service.DeleteSpaceRole(id)).ToActionResult();

    [HttpPost("assign-permissions"), HasPermission(Permissions.Roles.Assign)]
    public async Task<IActionResult> AssignSpacePermissions(AssignSpaceRolePermissionsRequestDto request) =>
        (await service.AssignSpacePermissions(request)).ToActionResult();

    [HttpGet("/api/v1/space-permissions")]
    public async Task<IActionResult> GetSpacePermissions([FromQuery] PaginationRequest pagination) =>
        (await service.GetSpacePermissions(pagination)).ToActionResult();
}
