using HsumChaint.Shared;
using HsumChaint.Domain.Features.RolePermission.DTOs;

namespace HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;

public interface IRolePermissionService
{
    Task<PagedResult<RoleDto>> GetRoles(PaginationRequest? pagination = null);
    Task<Result<RoleDto>> GetRoleById(int id);
    Task<Result<RoleDto>> CreateRole(CreateRoleRequestDto request);
    Task<Result<RoleDto>> UpdateRole(int id, UpdateRoleRequestDto request);
    Task<Result<bool>> DeleteRole(int id);
    Task<PagedResult<PermissionDto>> GetPermissions(PaginationRequest? pagination = null);
    Task<Result<bool>> AssignPermissions(AssignRolePermissionsRequestDto request);
    Task<List<string>> GetUserPermissions(int userId);
}
