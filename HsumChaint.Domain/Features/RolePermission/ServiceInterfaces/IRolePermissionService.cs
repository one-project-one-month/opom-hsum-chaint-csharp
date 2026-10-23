using HsumChaint.Shared;
using HsumChaint.Domain.Features.RolePermission.DTOs;

namespace HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;

public interface IRolePermissionService
{
    Task<PagedResult<RoleDto>> GetRoles(PaginationRequest? pagination = null);
    Task<Result<RoleDto>> GetRole(GetRoleRequestDto request);
    Task<Result<RoleDto>> CreateRole(CreateRoleRequestDto request);
    Task<Result<RoleDto>> UpdateRole(UpdateRoleRequestDto request);
    Task<Result<bool>> DeleteRole(DeleteRoleRequestDto request);
    Task<PagedResult<PermissionDto>> GetPermissions(PaginationRequest? pagination = null);
    Task<Result<bool>> AssignPermissions(AssignRolePermissionsRequestDto request);
    Task<List<string>> GetUserPermissions(int userId);

    Task<PagedResult<SpaceRoleDto>> GetSpaceRoles(PaginationRequest? pagination = null);
    Task<Result<SpaceRoleDto>> GetSpaceRole(int id);
    Task<Result<SpaceRoleDto>> CreateSpaceRole(CreateSpaceRoleRequestDto request);
    Task<Result<SpaceRoleDto>> UpdateSpaceRole(int id, UpdateSpaceRoleRequestDto request);
    Task<Result<bool>> DeleteSpaceRole(int id);
    Task<PagedResult<SpacePermissionDto>> GetSpacePermissions(PaginationRequest? pagination = null);
    Task<Result<bool>> AssignSpacePermissions(AssignSpaceRolePermissionsRequestDto request);
}
