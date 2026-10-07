using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.RolePermission.DTOs;
using HsumChaint.Domain.Features.RolePermission.Services;
using HsumChaint.Shared.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HsumChaint.Tests;

public class RolePermissionServiceTests
{
    private static AppDbContext Context()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        RbacTestData.Seed(db);
        return db;
    }

    [Fact]
    public async Task Crud_ReplacesAndReactivatesPermissions_ThenSoftDeletesRole()
    {
        await using var db = Context();
        var service = new RolePermissionService(db);
        var created = await service.CreateRole(new() { Name = "Reviewer", PermissionIds = new() { 11, 13, 13 } });
        Assert.True(created.IsSuccess);
        Assert.Equal(2, created.Data!.Permissions.Count);
        var id = created.Data.Id;
        Assert.True((await service.UpdateRole(id, new() { Name = "Custom Reviewer", PermissionIds = new() { 13 } })).IsSuccess);
        Assert.True((await service.AssignPermissions(new() { RoleId = id, PermissionIds = new() { 11 } })).IsSuccess);
        Assert.Equal(2, await db.RolePermissions.CountAsync(rp => rp.RoleId == id));
        Assert.Equal("Donation.View", (await service.GetRoleById(id)).Data!.Permissions.Single().Name);
        Assert.Contains((await service.GetRoles()).Data!, r => r.Id == id && r.Name == "Custom Reviewer");
        var permissions = await service.GetPermissions();
        Assert.Equal(10, permissions.Data.Count);
        Assert.Equal(Permissions.All.Count, permissions.Pagination.TotalCount);
        Assert.True((await service.DeleteRole(id)).Data);
        Assert.False((await service.GetRoleById(id)).IsSuccess);
    }

    [Theory]
    [InlineData(1, "Admin")]
    [InlineData(1, "RenamedAdmin")]
    [InlineData(20, "Admin")]
    public async Task AdminGuard_ProtectsIdAndName(int id, string name)
    {
        await using var db = Context();
        var admin = await db.Roles.FindAsync(1);
        admin!.Name = "OriginalAdmin";
        if (id == 1) admin.Name = name;
        else db.Roles.Add(new Role { Id = id, Name = name });
        await db.SaveChangesAsync();
        var service = new RolePermissionService(db);
        Assert.False((await service.DeleteRole(id)).IsSuccess);
        Assert.False((await service.UpdateRole(id, new() { Name = "Other", PermissionIds = Enumerable.Range(1, Permissions.All.Count).ToList() })).IsSuccess);
        Assert.False((await service.AssignPermissions(new() { RoleId = id, PermissionIds = new() })).IsSuccess);
    }

    [Fact]
    public async Task AdminGuard_AllowsFullAssignmentAndAddition()
    {
        await using var db = Context();
        db.Permissions.Add(new Permission { Id = 30, Name = "Custom.View" });
        await db.SaveChangesAsync();
        var service = new RolePermissionService(db);
        Assert.False((await service.AssignPermissions(new() { RoleId = 1, PermissionIds = Enumerable.Range(1, 15).ToList() })).IsSuccess);
        Assert.True((await service.AssignPermissions(new() { RoleId = 1, PermissionIds = Enumerable.Range(1, 15).Append(30).ToList() })).IsSuccess);
        Assert.Equal(16, (await service.GetRoleById(1)).Data!.Permissions.Count);
    }

    [Fact]
    public async Task AdminGuard_AllowsRemovingInactivePermissionMappings()
    {
        await using var db = Context();
        db.Permissions.Find(1)!.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.True((await new RolePermissionService(db).AssignPermissions(new()
        {
            RoleId = 1, PermissionIds = Enumerable.Range(2, 14).ToList()
        })).IsSuccess);
    }

    [Fact]
    public async Task InvalidRequests_DoNotChangeRole()
    {
        await using var db = Context();
        var service = new RolePermissionService(db);
        Assert.False((await service.CreateRole(new() { Name = " admin ", PermissionIds = new() })).IsSuccess);
        Assert.False((await service.CreateRole(new() { Name = " ", PermissionIds = new() })).IsSuccess);
        Assert.False((await service.CreateRole(new() { Name = "Invalid", PermissionIds = new() { 999 } })).IsSuccess);
        db.Permissions.Find(11)!.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.False((await service.AssignPermissions(new() { RoleId = 3, PermissionIds = new() { 11 } })).IsSuccess);
        Assert.False((await service.UpdateRole(3, new() { Name = "Changed", PermissionIds = new() { 999 } })).IsSuccess);
        Assert.Equal("User", db.Roles.Find(3)!.Name);
        Assert.False((await service.GetRoleById(999)).IsSuccess);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("member")]
    [InlineData("invitation")]
    public async Task DeleteRole_RejectsReferencedRole(string reference)
    {
        await using var db = Context();
        if (reference == "user") db.Users.Add(new User { Name = "User", PhoneNumber = "091", Password = "pw", RoleId = 3, IsDeleted = false });
        if (reference == "member") db.MonasteryMembers.Add(new MonasteryMember { RoleId = 3 });
        if (reference == "invitation") db.Invitations.Add(new Invitation { RoleId = 3, Status = Shared.CommonEnum.InvitationStatus.Pending });
        await db.SaveChangesAsync();
        Assert.False((await new RolePermissionService(db).DeleteRole(3)).IsSuccess);
        Assert.False(db.Roles.Find(3)!.IsDeleted);
    }

    [Fact]
    public async Task GetUserPermissions_FiltersDeletedAssignmentsPermissionsRolesAndUsers()
    {
        await using var db = Context();
        var user = new User { Id = 100, Name = "Admin", PhoneNumber = "091", Password = "pw", RoleId = 1, IsDeleted = false };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new RolePermissionService(db);
        Assert.Equal(Permissions.All.OrderBy(p => p), await service.GetUserPermissions(100));
        db.RolePermissions.First(rp => rp.RoleId == 1 && rp.PermissionId == 1).IsDeleted = true;
        db.Permissions.Find(2)!.IsDeleted = true;
        await db.SaveChangesAsync();
        var permissions = await service.GetUserPermissions(100);
        Assert.DoesNotContain(Permissions.Roles.View, permissions);
        Assert.DoesNotContain(Permissions.Roles.Manage, permissions);
        db.Roles.Find(1)!.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.Empty(await service.GetUserPermissions(100));
        db.Roles.Find(1)!.IsDeleted = false;
        user.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.Empty(await service.GetUserPermissions(100));
    }
}
