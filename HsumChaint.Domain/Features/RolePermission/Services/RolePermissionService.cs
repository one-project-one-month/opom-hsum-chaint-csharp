using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.RolePermission.DTOs;
using HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;
using RolePermissionEntity = HsumChaint.Database.Models.RolePermission;

namespace HsumChaint.Domain.Features.RolePermission.Services;

public class RolePermissionService(AppDbContext db) : IRolePermissionService
{
    private IQueryable<Role> ActiveRoles => db.Roles.Where(r => !r.IsDeleted);
    private IQueryable<Role> RolesWithPermissions => ActiveRoles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission);

    public async Task<ApplicationCommonResponseModel<List<RoleDto>>> GetRoles()
    {
        var roles = (await RolesWithPermissions.AsNoTracking().OrderBy(r => r.Name).ToListAsync()).Select(Map).ToList();
        return new() { IsSuccess = true, ListData = roles, Message = "Roles retrieved successfully." };
    }

    public async Task<ApplicationCommonResponseModel<RoleDto>> GetRoleById(int id)
    {
        var role = await RolesWithPermissions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return role == null ? Fail<RoleDto>("Role not found.") : Success(Map(role));
    }

    public async Task<ApplicationCommonResponseModel<RoleDto>> CreateRole(CreateRoleRequestDto request)
    {
        var error = await Validate(request.Name, request.PermissionIds);
        if (error != null) return Fail<RoleDto>(error);
        var role = new Role { Name = request.Name.Trim() };
        foreach (var id in request.PermissionIds.Distinct())
            role.RolePermissions.Add(new RolePermissionEntity { PermissionId = id });
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return await GetRoleById(role.Id);
    }

    public async Task<ApplicationCommonResponseModel<RoleDto>> UpdateRole(int id, UpdateRoleRequestDto request)
    {
        var role = await RolesWithPermissions.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return Fail<RoleDto>("Role not found.");
        var error = await Validate(request.Name, request.PermissionIds, id);
        if (error != null) return Fail<RoleDto>(error);
        if (IsAdmin(role) && (request.Name.Trim() != role.Name || await StripsAdminPermissions(role, request.PermissionIds)))
            return Fail<RoleDto>("The system Admin role cannot be renamed or have permissions removed.");
        role.Name = request.Name.Trim();
        ReplacePermissions(role, request.PermissionIds);
        await db.SaveChangesAsync();
        return await GetRoleById(id);
    }

    public async Task<ApplicationCommonResponseModel<bool>> DeleteRole(int id)
    {
        var role = await ActiveRoles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return Fail<bool>("Role not found.");
        if (IsAdmin(role)) return Fail<bool>("The system Admin role cannot be deleted.");
        if (await db.Users.AnyAsync(u => u.RoleId == id && u.IsDeleted == false)
            || await db.MonasteryMembers.AnyAsync(m => m.RoleId == id)
            || await db.Invitations.AnyAsync(i => i.RoleId == id && i.Status == HsumChaint.Shared.CommonEnum.InvitationStatus.Pending))
            return Fail<bool>("Role is assigned to users, members or pending invitations.");
        role.IsDeleted = true;
        await db.SaveChangesAsync();
        return Success(true);
    }

    public async Task<ApplicationCommonResponseModel<List<PermissionDto>>> GetPermissions() => new()
    {
        IsSuccess = true,
        ListData = await db.Permissions.Where(p => !p.IsDeleted).OrderBy(p => p.Name)
            .Select(p => new PermissionDto { Id = p.Id, Name = p.Name }).ToListAsync()
    };

    public async Task<ApplicationCommonResponseModel<bool>> AssignPermissions(AssignRolePermissionsRequestDto request)
    {
        var role = await RolesWithPermissions.FirstOrDefaultAsync(r => r.Id == request.RoleId);
        if (role == null) return Fail<bool>("Role not found.");
        if (!await ValidPermissions(request.PermissionIds)) return Fail<bool>("One or more permissions are invalid or inactive.");
        if (IsAdmin(role) && await StripsAdminPermissions(role, request.PermissionIds))
            return Fail<bool>("The system Admin role cannot have permissions removed.");
        ReplacePermissions(role, request.PermissionIds);
        await db.SaveChangesAsync();
        return Success(true);
    }

    public async Task<List<string>> GetUserPermissions(int userId) => await (
        from user in db.Users
        join role in db.Roles on user.RoleId equals role.Id
        join rp in db.RolePermissions on role.Id equals rp.RoleId
        join permission in db.Permissions on rp.PermissionId equals permission.Id
        where user.Id == userId && user.IsDeleted == false && !role.IsDeleted && !rp.IsDeleted && !permission.IsDeleted
        select permission.Name).Distinct().OrderBy(name => name).ToListAsync();

    private async Task<string?> Validate(string name, List<int>? ids, int? roleId = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 50) return "Role name is required and must not exceed 50 characters.";
        var normalized = name.Trim().ToUpperInvariant();
        if (await db.Roles.AnyAsync(r => r.Id != roleId && r.Name.ToUpper() == normalized)) return "Role name already exists.";
        if (!await ValidPermissions(ids)) return "One or more permissions are invalid or inactive.";
        return null;
    }

    private async Task<bool> ValidPermissions(List<int>? ids) => ids != null &&
        await db.Permissions.CountAsync(p => ids.Contains(p.Id) && !p.IsDeleted) == ids.Distinct().Count();

    private async Task<bool> StripsAdminPermissions(Role role, List<int> ids)
    {
        var required = await db.Permissions.Where(p => !p.IsDeleted).Select(p => p.Id).ToListAsync();
        return required.Concat(role.RolePermissions.Where(rp => !rp.IsDeleted && !rp.Permission.IsDeleted).Select(rp => rp.PermissionId)).Except(ids).Any();
    }

    private static bool IsAdmin(Role role) => role.Id == 1 || string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase);

    private static void ReplacePermissions(Role role, List<int> ids)
    {
        var desired = ids.ToHashSet();
        foreach (var rp in role.RolePermissions) rp.IsDeleted = !desired.Contains(rp.PermissionId);
        foreach (var id in desired.Except(role.RolePermissions.Select(rp => rp.PermissionId)))
            role.RolePermissions.Add(new RolePermissionEntity { RoleId = role.Id, PermissionId = id });
    }

    private static RoleDto Map(Role role) => new()
    {
        Id = role.Id, Name = role.Name,
        Permissions = role.RolePermissions.Where(rp => !rp.IsDeleted && !rp.Permission.IsDeleted)
            .Select(rp => new PermissionDto { Id = rp.PermissionId, Name = rp.Permission.Name }).OrderBy(p => p.Name).ToList()
    };

    private static ApplicationCommonResponseModel<T> Fail<T>(string message) => new() { IsSuccess = false, Message = message };
    private static ApplicationCommonResponseModel<T> Success<T>(T data) => new() { IsSuccess = true, Data = data, Message = "Successful." };
}
