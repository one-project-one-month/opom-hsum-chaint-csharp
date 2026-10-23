using System;
using System.Linq;
using System.Threading.Tasks;
using HsumChaint.API.Authorization;
using HsumChaint.API.Features.Monastery.Controllers;
using HsumChaint.API.Features.RolePermission.Controllers;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Monastery.Services;
using HsumChaint.Domain.Features.RolePermission.DTOs;
using HsumChaint.Domain.Features.RolePermission.Services;
using HsumChaint.Shared;
using HsumChaint.Shared.Authorization;
using HsumChaint.Shared.CommonEnum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HsumChaint.Tests;

public class SpaceRolePermissionTests
{
    private static AppDbContext CreateContext()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        RbacTestData.Seed(db);
        return db;
    }

    [Fact]
    public async Task SpaceRoles_List_ReturnsAvailableRolesWithPagination()
    {
        await using var db = CreateContext();
        var service = new RolePermissionService(db);

        var result = await service.GetSpaceRoles(new PaginationRequest { PageNumber = 1, PageSize = 10 });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Count >= 4);
        Assert.Contains(result.Data, r => r.Id == SpaceRoles.OwnerId && r.Name == SpaceRoles.Owner);
        Assert.Contains(result.Data, r => r.Id == SpaceRoles.AdminId && r.Name == SpaceRoles.Admin);
        Assert.Contains(result.Data, r => r.Id == SpaceRoles.EditorId && r.Name == SpaceRoles.Editor);
        Assert.Contains(result.Data, r => r.Id == SpaceRoles.ViewerId && r.Name == SpaceRoles.Viewer);

        var ownerRole = result.Data.First(r => r.Id == SpaceRoles.OwnerId);
        Assert.Equal(SpacePermissions.All.Count, ownerRole.Permissions.Count);
    }

    [Fact]
    public async Task SpaceRoles_Crud_CreatesUpdatesAndDeletesCustomRole()
    {
        await using var db = CreateContext();
        var service = new RolePermissionService(db);

        var donationView = db.SpacePermissions.Single(p => p.Name == SpacePermissions.DonationView);
        var donationCreate = db.SpacePermissions.Single(p => p.Name == SpacePermissions.DonationCreate);

        // 1. Create custom space role
        var createResult = await service.CreateSpaceRole(new CreateSpaceRoleRequestDto
        {
            Name = "Treasurer",
            Description = "Monastery accountant and treasurer",
            PermissionIds = new() { donationView.Id, donationCreate.Id }
        });

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Data);
        Assert.Equal("Treasurer", createResult.Data.Name);
        Assert.Equal("Monastery accountant and treasurer", createResult.Data.Description);
        Assert.Equal(2, createResult.Data.Permissions.Count);

        var customRoleId = createResult.Data.Id;

        // 2. Get single space role
        var getResult = await service.GetSpaceRole(customRoleId);
        Assert.True(getResult.IsSuccess);
        Assert.Equal("Treasurer", getResult.Data!.Name);

        // 3. Update space role name, description, and permissions
        var updateResult = await service.UpdateSpaceRole(customRoleId, new UpdateSpaceRoleRequestDto
        {
            Name = "Senior Treasurer",
            Description = "Updated description",
            PermissionIds = new() { donationView.Id }
        });

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Senior Treasurer", updateResult.Data!.Name);
        Assert.Equal("Updated description", updateResult.Data.Description);
        Assert.Single(updateResult.Data.Permissions);

        // 4. Assign permissions
        var assignResult = await service.AssignSpacePermissions(new AssignSpaceRolePermissionsRequestDto
        {
            SpaceRoleId = customRoleId,
            PermissionIds = new() { donationView.Id, donationCreate.Id }
        });

        Assert.True(assignResult.IsSuccess);
        var reloaded = await service.GetSpaceRole(customRoleId);
        Assert.Equal(2, reloaded.Data!.Permissions.Count);

        // 5. Delete space role
        var deleteResult = await service.DeleteSpaceRole(customRoleId);
        Assert.True(deleteResult.IsSuccess);
        Assert.True(deleteResult.Data);

        // 6. Deleted role is not found
        var getDeleted = await service.GetSpaceRole(customRoleId);
        Assert.False(getDeleted.IsSuccess);
    }

    [Fact]
    public async Task SpaceRoles_OwnerProtection_PreventsDeletingRenamingOrStrippingPermissions()
    {
        await using var db = CreateContext();
        var service = new RolePermissionService(db);

        // 1. Cannot delete Owner role
        var deleteOwner = await service.DeleteSpaceRole(SpaceRoles.OwnerId);
        Assert.False(deleteOwner.IsSuccess);
        Assert.Contains("Owner role cannot be deleted", deleteOwner.Message);

        // 2. Cannot rename Owner role
        var renameOwner = await service.UpdateSpaceRole(SpaceRoles.OwnerId, new UpdateSpaceRoleRequestDto
        {
            Name = "SuperOwner",
            PermissionIds = Enumerable.Range(1, SpacePermissions.All.Count).ToList()
        });
        Assert.False(renameOwner.IsSuccess);
        Assert.Contains("Owner role cannot be renamed", renameOwner.Message);

        // 3. Cannot strip permissions from Owner role
        var stripPermissions = await service.AssignSpacePermissions(new AssignSpaceRolePermissionsRequestDto
        {
            SpaceRoleId = SpaceRoles.OwnerId,
            PermissionIds = new() { 1 } // Only 1 permission instead of all
        });
        Assert.False(stripPermissions.IsSuccess);
        Assert.Contains("cannot have permissions removed", stripPermissions.Message);
    }

    [Fact]
    public async Task SpaceRoles_Delete_RejectsAssignedRoles()
    {
        await using var db = CreateContext();
        var service = new RolePermissionService(db);

        // Add custom role
        var customRole = new SpaceRole { Id = 50, Name = "Helper", Description = "Helper role" };
        db.SpaceRoles.Add(customRole);

        // Assign to monastery member
        db.MonasteryMembers.Add(new MonasteryMember
        {
            UserId = 1,
            MonasterySpaceId = 10,
            SpaceRoleId = 50
        });
        await db.SaveChangesAsync();

        // 1. Cannot delete role assigned to member
        var deleteAssigned = await service.DeleteSpaceRole(50);
        Assert.False(deleteAssigned.IsSuccess);
        Assert.Contains("assigned to monastery members", deleteAssigned.Message);

        // Remove from member, but assign to pending invitation
        db.MonasteryMembers.Remove(db.MonasteryMembers.Single(m => m.SpaceRoleId == 50));
        db.Invitations.Add(new Invitation
        {
            MonasterySpaceId = 10,
            InvitedUserId = 2,
            SpaceRoleId = 50,
            Status = InvitationStatus.Pending
        });
        await db.SaveChangesAsync();

        // 2. Cannot delete role assigned to pending invitation
        var deleteInvited = await service.DeleteSpaceRole(50);
        Assert.False(deleteInvited.IsSuccess);
        Assert.Contains("assigned to pending invitations", deleteInvited.Message);
    }

    [Fact]
    public async Task SpaceRoles_Validation_RejectsInvalidInputs()
    {
        await using var db = CreateContext();
        var service = new RolePermissionService(db);

        // 1. Empty name
        Assert.False((await service.CreateSpaceRole(new() { Name = "   " })).IsSuccess);

        // 2. Duplicate name (case-insensitive)
        Assert.False((await service.CreateSpaceRole(new() { Name = "admin" })).IsSuccess);

        // 3. Invalid permission ID
        Assert.False((await service.CreateSpaceRole(new() { Name = "UniqueRole", PermissionIds = new() { 999 } })).IsSuccess);

        // 4. Update non-existent role
        Assert.False((await service.UpdateSpaceRole(999, new() { Name = "Ghost" })).IsSuccess);

        // 5. Delete non-existent role
        Assert.False((await service.DeleteSpaceRole(999)).IsSuccess);
    }

    [Fact]
    public async Task Monastery_SpacePermissions_VisibilityRules_EnforceOwnerVsMember()
    {
        await using var db = CreateContext();
        var monasteryService = new MonasteryService(db);

        // Setup users and monastery
        var ownerUser = new User { Id = 1, Name = "Sayadaw", PhoneNumber = "091", Password = "pw", RoleId = 2, IsDeleted = false };
        var viewerUser = new User { Id = 2, Name = "Ko Aung", PhoneNumber = "092", Password = "pw", RoleId = 3, IsDeleted = false };
        var editorUser = new User { Id = 3, Name = "Ko Ba", PhoneNumber = "093", Password = "pw", RoleId = 3, IsDeleted = false };
        var outsideUser = new User { Id = 4, Name = "Stranger", PhoneNumber = "094", Password = "pw", RoleId = 3, IsDeleted = false };
        db.Users.AddRange(ownerUser, viewerUser, editorUser, outsideUser);

        var monastery = new MonasterySpace { Id = 10, MonasteryName = "Mahagandhayon", CreatedById = 1, IsDeleted = false };
        db.MonasterySpaces.Add(monastery);

        db.MonasteryMembers.AddRange(
            new MonasteryMember { UserId = 1, MonasterySpaceId = 10, SpaceRoleId = SpaceRoles.OwnerId, IsOwner = true },
            new MonasteryMember { UserId = 2, MonasterySpaceId = 10, SpaceRoleId = SpaceRoles.ViewerId, IsOwner = false },
            new MonasteryMember { UserId = 3, MonasterySpaceId = 10, SpaceRoleId = SpaceRoles.EditorId, IsOwner = false }
        );
        await db.SaveChangesAsync();

        // 1. Space Owner: Can view all permissions available in that Space
        var ownerPerms = await monasteryService.GetSpacePermissions(1, 10);
        Assert.True(ownerPerms.IsSuccess);
        Assert.Equal(SpacePermissions.All.Count, ownerPerms.Data!.Count);
        Assert.Contains(ownerPerms.Data, p => p.Name == SpacePermissions.SpaceDelete);
        Assert.Contains(ownerPerms.Data, p => p.Name == SpacePermissions.SpaceTransferOwnership);

        // 2. Viewer Member: Can view ONLY the permissions associated with their own role
        var viewerPerms = await monasteryService.GetSpacePermissions(2, 10);
        Assert.True(viewerPerms.IsSuccess);
        Assert.Equal(3, viewerPerms.Data!.Count); // Space.View, Donation.View, Donation.Create
        Assert.Contains(viewerPerms.Data, p => p.Name == SpacePermissions.SpaceView);
        Assert.Contains(viewerPerms.Data, p => p.Name == SpacePermissions.DonationView);
        Assert.Contains(viewerPerms.Data, p => p.Name == SpacePermissions.DonationCreate);
        Assert.DoesNotContain(viewerPerms.Data, p => p.Name == SpacePermissions.SpaceDelete);
        Assert.DoesNotContain(viewerPerms.Data, p => p.Name == SpacePermissions.DonationReview);

        // 3. Editor Member: Can view ONLY Editor permissions
        var editorPerms = await monasteryService.GetSpacePermissions(3, 10);
        Assert.True(editorPerms.IsSuccess);
        Assert.Contains(editorPerms.Data!, p => p.Name == SpacePermissions.DonationSchedule);
        Assert.DoesNotContain(editorPerms.Data!, p => p.Name == SpacePermissions.SpaceUpdateMemberRole);

        // 4. Non-member user: Access denied / unauthorized
        var strangerPerms = await monasteryService.GetSpacePermissions(4, 10);
        Assert.False(strangerPerms.IsSuccess);
        Assert.Contains("not authorized", strangerPerms.Message);

        // 5. Non-existent monastery: Not found
        var nonExistent = await monasteryService.GetSpacePermissions(1, 999);
        Assert.False(nonExistent.IsSuccess);
        Assert.Contains("not found", nonExistent.Message);
    }

    [Fact]
    public async Task Monastery_GetSpaceRoles_ReturnsRolesForMembers_RejectsNonMembers()
    {
        await using var db = CreateContext();
        var monasteryService = new MonasteryService(db);

        var memberUser = new User { Id = 1, Name = "Member", PhoneNumber = "091", Password = "pw", RoleId = 3, IsDeleted = false };
        var strangerUser = new User { Id = 2, Name = "Stranger", PhoneNumber = "092", Password = "pw", RoleId = 3, IsDeleted = false };
        db.Users.AddRange(memberUser, strangerUser);

        var monastery = new MonasterySpace { Id = 20, MonasteryName = "Kyauk Gu", CreatedById = 1, IsDeleted = false };
        db.MonasterySpaces.Add(monastery);

        db.MonasteryMembers.Add(new MonasteryMember { UserId = 1, MonasterySpaceId = 20, SpaceRoleId = SpaceRoles.AdminId, IsOwner = false });
        await db.SaveChangesAsync();

        // 1. Member can view available space roles
        var rolesResult = await monasteryService.GetSpaceRoles(1, 20);
        Assert.True(rolesResult.IsSuccess);
        Assert.True(rolesResult.Data!.Count >= 4);

        // 2. Non-member is rejected
        var strangerResult = await monasteryService.GetSpaceRoles(2, 20);
        Assert.False(strangerResult.IsSuccess);
        Assert.Contains("not authorized", strangerResult.Message);
    }

    [Fact]
    public void ControllerAttributes_VerifyAuthorizationAndRouting()
    {
        // 1. SpaceRolesController routing and permissions
        var controllerType = typeof(SpaceRolesController);
        var routeAttr = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: false).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(routeAttr);
        Assert.Equal("api/v1/space-roles", routeAttr.Template);

        // Verify create requires Roles.Manage
        var createMethod = controllerType.GetMethod(nameof(SpaceRolesController.CreateSpaceRole));
        Assert.NotNull(createMethod);
        var createPerm = createMethod!.GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(createPerm);
        Assert.Equal(Permissions.Roles.Manage, createPerm.Policy);

        // Verify assign-permissions requires Roles.Assign
        var assignMethod = controllerType.GetMethod(nameof(SpaceRolesController.AssignSpacePermissions));
        Assert.NotNull(assignMethod);
        var assignPerm = assignMethod!.GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(assignPerm);
        Assert.Equal(Permissions.Roles.Assign, assignPerm.Policy);

        // 2. MonasteriesController permissions endpoint
        var monasteryControllerType = typeof(MonasteriesController);
        var getPermsMethod = monasteryControllerType.GetMethod(nameof(MonasteriesController.GetPermissions));
        Assert.NotNull(getPermsMethod);
        var getPermsRoute = getPermsMethod!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false).Cast<HttpGetAttribute>().FirstOrDefault();
        Assert.NotNull(getPermsRoute);
        Assert.Equal("{id}/permissions", getPermsRoute.Template);

        var getRolesMethod = monasteryControllerType.GetMethod(nameof(MonasteriesController.GetRoles));
        Assert.NotNull(getRolesMethod);
        var getRolesRoute = getRolesMethod!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false).Cast<HttpGetAttribute>().FirstOrDefault();
        Assert.NotNull(getRolesRoute);
        Assert.Equal("{id}/roles", getRolesRoute.Template);
    }
}
