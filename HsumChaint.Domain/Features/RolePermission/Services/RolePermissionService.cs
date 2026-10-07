using HsumChaint.Shared;
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

    public async Task<PagedResult<RoleDto>> GetRoles(PaginationRequest? pagination = null)
    {
        var pageNumber = pagination?.PageNumber ?? 1;
        var pageSize = pagination?.PageSize ?? 10;
        if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
        {
            return PagedResult<RoleDto>.Failure("Page number and page size must be positive and within the supported range.");
        }

        var query = RolesWithPermissions.AsNoTracking();
        var totalCount = await query.CountAsync();
        var roles = (await query.OrderBy(r => r.Name).ThenBy(r => r.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync()).Select(Map).ToList();
        return PagedResult<RoleDto>.Success(roles, new Pagination(pageNumber, pageSize, totalCount), "Roles retrieved successfully.");
    }

    public async Task<Result<RoleDto>> GetRoleById(int id)
    {
        var role = await RolesWithPermissions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return role == null ? Result<RoleDto>.Failure("Role not found.") : Result<RoleDto>.Success(Map(role), "Successful.");
    }

    public async Task<Result<RoleDto>> CreateRole(CreateRoleRequestDto request)
    {
        var error = await Validate(request.Name, request.PermissionIds);
        if (error != null) return Result<RoleDto>.Failure(error);
        var role = new Role { Name = request.Name.Trim() };
        foreach (var id in request.PermissionIds.Distinct())
            role.RolePermissions.Add(new RolePermissionEntity { PermissionId = id });
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return await GetRoleById(role.Id);
    }

    public async Task<Result<RoleDto>> UpdateRole(int id, UpdateRoleRequestDto request)
    {
        var role = await RolesWithPermissions.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return Result<RoleDto>.Failure("Role not found.");
        var error = await Validate(request.Name, request.PermissionIds, id);
        if (error != null) return Result<RoleDto>.Failure(error);
        if (IsAdmin(role) && (request.Name.Trim() != role.Name || await StripsAdminPermissions(role, request.PermissionIds)))
            return Result<RoleDto>.Failure("The system Admin role cannot be renamed or have permissions removed.");
        role.Name = request.Name.Trim();
        ReplacePermissions(role, request.PermissionIds);
        await db.SaveChangesAsync();
        return await GetRoleById(id);
    }

    public async Task<Result<bool>> DeleteRole(int id)
    {
        var role = await ActiveRoles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return Result<bool>.Failure("Role not found.");
        if (IsAdmin(role)) return Result<bool>.Failure("The system Admin role cannot be deleted.");
        if (await db.Users.AnyAsync(u => u.RoleId == id && u.IsDeleted == false)
            || await db.MonasteryMembers.AnyAsync(m => m.RoleId == id)
            || await db.Invitations.AnyAsync(i => i.RoleId == id && i.Status == HsumChaint.Shared.CommonEnum.InvitationStatus.Pending))
            return Result<bool>.Failure("Role is assigned to users, members or pending invitations.");
        role.IsDeleted = true;
        await db.SaveChangesAsync();
        return Result<bool>.Success(true, "Successful.");
    }

    public async Task<PagedResult<PermissionDto>> GetPermissions(PaginationRequest? pagination = null)
    {
        var pageNumber = pagination?.PageNumber ?? 1;
        var pageSize = pagination?.PageSize ?? 10;
        if (pageNumber < 1 || pageSize < 1 || ((long)pageNumber - 1) * pageSize > int.MaxValue)
        {
            return PagedResult<PermissionDto>.Failure("Page number and page size must be positive and within the supported range.");
        }

        var query = db.Permissions.AsNoTracking().Where(p => !p.IsDeleted);
        var totalCount = await query.CountAsync();
        var permissions = await query.OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(p => new PermissionDto { Id = p.Id, Name = p.Name }).ToListAsync();
        return PagedResult<PermissionDto>.Success(permissions, new Pagination(pageNumber, pageSize, totalCount), "Permissions retrieved successfully.");
    }

    public async Task<Result<bool>> AssignPermissions(AssignRolePermissionsRequestDto request)
    {
        var role = await RolesWithPermissions.FirstOrDefaultAsync(r => r.Id == request.RoleId);
        if (role == null) return Result<bool>.Failure("Role not found.");
        if (!await ValidPermissions(request.PermissionIds)) return Result<bool>.Failure("One or more permissions are invalid or inactive.");
        if (IsAdmin(role) && await StripsAdminPermissions(role, request.PermissionIds))
            return Result<bool>.Failure("The system Admin role cannot have permissions removed.");
        ReplacePermissions(role, request.PermissionIds);
        await db.SaveChangesAsync();
        return Result<bool>.Success(true, "Successful.");
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

}
