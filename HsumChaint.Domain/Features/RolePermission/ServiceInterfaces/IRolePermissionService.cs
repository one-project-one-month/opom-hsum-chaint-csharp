using HsumChaint.Domain.Features.RolePermission.DTOs;

namespace HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;

public interface IRolePermissionService
{
    Task<ApplicationCommonResponseModel<List<RoleDto>>> GetRoles();
    Task<ApplicationCommonResponseModel<RoleDto>> GetRoleById(int id);
    Task<ApplicationCommonResponseModel<RoleDto>> CreateRole(CreateRoleRequestDto request);
    Task<ApplicationCommonResponseModel<RoleDto>> UpdateRole(int id, UpdateRoleRequestDto request);
    Task<ApplicationCommonResponseModel<bool>> DeleteRole(int id);
    Task<ApplicationCommonResponseModel<List<PermissionDto>>> GetPermissions();
    Task<ApplicationCommonResponseModel<bool>> AssignPermissions(AssignRolePermissionsRequestDto request);
    Task<List<string>> GetUserPermissions(int userId);
}
