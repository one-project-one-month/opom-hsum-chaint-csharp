using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Monastery.DTOs;
using HsumChaint.Domain.Features.Monastery.Services;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HsumChaint.Tests;

public class MonasteryServiceTests
{
    [Fact]
    public async Task CustomRole_ManageMembersPermission_ControlsMembershipOperations()
    {
        await using var db = CreateDbContext();
        SeedUser(db, 1, "Owner", "091");
        SeedUser(db, 2, "Custom Manager", "092");
        SeedUser(db, 3, "Invitee", "093");
        SeedMonastery(db, 10, 1);
        db.Roles.Add(new Role { Id = 20, Name = "Custom Manager" });
        db.MonasteryMembers.Add(new MonasteryMember { UserId = 2, MonasterySpaceId = 10, RoleId = 20 });
        await db.SaveChangesAsync();
        var service = new MonasteryService(db);
        var request = new InviteMemberRequestDto { UserId = 3, RoleId = 5 };
        Assert.False((await service.InviteMember(2, 10, request)).IsSuccess);
        var permission = db.Permissions.Single(p => p.Name == "Monastery.ManageMembers");
        var mapping = new RolePermission { RoleId = 20, PermissionId = permission.Id };
        db.RolePermissions.Add(mapping);
        await db.SaveChangesAsync();
        Assert.True((await service.InviteMember(2, 10, request)).IsSuccess);
        Assert.Equal("Viewer", (await service.RespondToInvitation(3, db.Invitations.Single().Id,
            new() { Status = InvitationStatus.Accept })).Data!.RoleName);
        Assert.True((await service.UpdateMemberRole(2, 10, 3, new() { RoleId = 4 })).IsSuccess);
        Assert.Equal("Editor", (await service.GetMembers(2, 10)).Data!.Single(m => m.UserId == 3).RoleName);
        mapping.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.False((await service.RemoveMember(2, 10, 3)).IsSuccess);
        Assert.False((await service.InviteMember(2, 99, request)).IsSuccess);
        Assert.True((await service.RemoveMember(1, 10, 3)).IsSuccess);
    }

    [Fact]
    public async Task CreateMonastery_CreatesOwnerMembership()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Owner", "091");
        await dbContext.SaveChangesAsync();
        var service = new MonasteryService(dbContext);

        var response = await service.CreateMonastery(1, new CreateMonasteryRequestDto
        {
            MonasteryName = "Aung Myae",
            Address = "Yangon"
        });

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(3, response.Data!.CurrentUserRoleId);
        Assert.Equal("User", response.Data.CurrentUserRoleName);
        Assert.True(dbContext.MonasteryMembers.Single().IsOwner);
    }

    [Fact]
    public async Task InviteMember_CreatesPendingInvitationForExistingUser()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Owner", "091");
        SeedUser(dbContext, 2, "Editor", "092");
        SeedMonastery(dbContext, 10, 1);
        await dbContext.SaveChangesAsync();
        var service = new MonasteryService(dbContext);

        var response = await service.InviteMember(1, 10, new InviteMemberRequestDto
        {
            UserId = 2,
            RoleId = 4
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(InvitationStatus.Pending, response.Data!.Status);
        Assert.Equal(4, response.Data.RoleId);
        Assert.Single(dbContext.Notifications);
    }

    [Fact]
    public async Task RespondToInvitation_Accept_CreatesMember()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Owner", "091");
        SeedUser(dbContext, 2, "Editor", "092");
        SeedMonastery(dbContext, 10, 1);
        dbContext.Invitations.Add(new Invitation
        {
            Id = 50,
            MonasterySpaceId = 10,
            InvitedUserId = 2,
            InvitedById = 1,
            RoleId = 4,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var service = new MonasteryService(dbContext);

        var response = await service.RespondToInvitation(2, 50, new RespondInvitationRequestDto
        {
            Status = InvitationStatus.Accept
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(InvitationStatus.Accept, response.Data!.Status);
        Assert.Contains(dbContext.MonasteryMembers, x => x.UserId == 2 && x.RoleId == 4);
    }

    [Fact]
    public async Task UpdateMemberRole_RejectsViewerActor()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Owner", "091");
        SeedUser(dbContext, 2, "Viewer", "092");
        SeedUser(dbContext, 3, "Editor", "093");
        SeedMonastery(dbContext, 10, 1);
        dbContext.MonasteryMembers.AddRange(
            new MonasteryMember { UserId = 2, MonasterySpaceId = 10, RoleId = 5, IsOwner = false },
            new MonasteryMember { UserId = 3, MonasterySpaceId = 10, RoleId = 4, IsOwner = false });
        await dbContext.SaveChangesAsync();
        var service = new MonasteryService(dbContext);

        var response = await service.UpdateMemberRole(2, 10, 3, new UpdateMemberRoleRequestDto
        {
            RoleId = 1
        });

        Assert.False(response.IsSuccess);
        Assert.Contains("not authorized", response.Message);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        RbacTestData.Seed(context);
        return context;
    }

    private static void SeedUser(AppDbContext dbContext, int id, string name, string phone)
    {
        dbContext.Users.Add(new HsumChaint.Database.Models.User
        {
            Id = id,
            Name = name,
            PhoneNumber = phone,
            Password = "pw",
            RoleId = 3,
            IsDeleted = false
        });
    }

    private static void SeedMonastery(AppDbContext dbContext, int monasteryId, int ownerUserId)
    {
        dbContext.MonasterySpaces.Add(new MonasterySpace
        {
            Id = monasteryId,
            MonasteryName = "Aung Myae",
            CreatedById = ownerUserId
        });
        dbContext.MonasteryMembers.Add(new MonasteryMember
        {
            UserId = ownerUserId,
            MonasterySpaceId = monasteryId,
            RoleId = 2,
            IsOwner = true
        });
    }
}
