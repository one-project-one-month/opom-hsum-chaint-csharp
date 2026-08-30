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
        Assert.Equal(MonasteryRole.Owner, response.Data!.CurrentUserRole);
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
            Role = MonasteryRole.Editor
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(InvitationStatus.Pending, response.Data!.Status);
        Assert.Equal(MonasteryRole.Editor, response.Data.Role);
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
            Role = MonasteryRole.Editor,
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
        Assert.Contains(dbContext.MonasteryMembers, x => x.UserId == 2 && x.Role == MonasteryRole.Editor);
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
            new MonasteryMember { UserId = 2, MonasterySpaceId = 10, Role = MonasteryRole.Viewer, IsOwner = false },
            new MonasteryMember { UserId = 3, MonasterySpaceId = 10, Role = MonasteryRole.Editor, IsOwner = false });
        await dbContext.SaveChangesAsync();
        var service = new MonasteryService(dbContext);

        var response = await service.UpdateMemberRole(2, 10, 3, new UpdateMemberRoleRequestDto
        {
            Role = MonasteryRole.Admin
        });

        Assert.False(response.IsSuccess);
        Assert.Contains("not authorized", response.Message);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedUser(AppDbContext dbContext, int id, string name, string phone)
    {
        dbContext.Users.Add(new HsumChaint.Database.Models.User
        {
            Id = id,
            Name = name,
            PhoneNumber = phone,
            Password = "pw",
            UserType = UserType.User,
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
            Role = MonasteryRole.Owner,
            IsOwner = true
        });
    }
}
