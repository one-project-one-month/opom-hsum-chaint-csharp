using System.Security.Claims;
using HsumChaint.API.Features.User.Controllers;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.User.DTOs;
using HsumChaint.Domain.Features.User.Services;
using HsumChaint.Shared.Authorization;
using HsumChaint.Shared.CommonEnum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using InfrastructureUser = HsumChaint.Database.Models.User;

namespace HsumChaint.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetAllUsers_ReturnsMappedUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            new InfrastructureUser { Id = 1, Name = "Alice", PhoneNumber = "1111111111", Password = "pw", RoleId = 3, IsDeleted = false },
            new InfrastructureUser { Id = 2, Name = "Bob", PhoneNumber = "2222222222", Password = "pw", RoleId = 3, IsDeleted = false });
        await dbContext.SaveChangesAsync();

        var userService = new UserService(dbContext);

        var response = await userService.GetAllUsers();

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data!.Count);
        Assert.Equal("Alice", response.Data.First().Name);
        Assert.Equal(1, response.Pagination.PageNumber);
        Assert.Equal(10, response.Pagination.PageSize);
        Assert.Equal(2, response.Pagination.TotalCount);
    }

    [Fact]
    public async Task PutUser_ReturnsSuccess_WhenUpdateSucceeds()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new InfrastructureUser
        {
            Id = 1,
            Name = "Original",
            PhoneNumber = "1111111111",
            Password = "pw",
            RoleId = 3,
            IsDeleted = false
        });
        await dbContext.SaveChangesAsync();

        var userService = new UserService(dbContext);

        var response = await userService.PutUser(new UserDto
        {
            Id = 1,
            Name = "Updated",
            PhoneNumber = "3333333333",
            RoleId = 3
        });

        Assert.True(response.IsSuccess);
        Assert.Equal("Updated", (await dbContext.Users.FindAsync(1))!.Name);
    }

    [Fact]
    public async Task GetUserInvitationList_ReturnsEnrichedInvitations()
    {
        await using var db = CreateDbContext();
        var monastery = new MonasterySpace { Id = 10, MonasteryName = "Mahagandhayon" };
        var inviter = new InfrastructureUser { Id = 2, Name = "Monk U Thawbana", PhoneNumber = "0922222222", Password = "pw", RoleId = 2, IsDeleted = false };
        var invitee = new InfrastructureUser { Id = 3, Name = "Ko Mg Mg", PhoneNumber = "0933333333", Password = "pw", RoleId = 3, IsDeleted = false };
        var spaceRole = await db.SpaceRoles.FindAsync(SpaceRoles.EditorId);

        db.MonasterySpaces.Add(monastery);
        db.Users.AddRange(inviter, invitee);

        db.Invitations.Add(new Invitation
        {
            Id = 1,
            MonasterySpaceId = 10,
            MonasterySpace = monastery,
            InvitedById = 2,
            InvitedBy = inviter,
            InvitedUserId = 3,
            InvitedUser = invitee,
            SpaceRoleId = SpaceRoles.EditorId,
            SpaceRole = spaceRole,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var result = await service.GetUserInvitationList(3);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        var inviteDto = result.Data!.First();
        Assert.Equal(1, inviteDto.Id);
        Assert.Equal(10, inviteDto.MonasterySpaceId);
        Assert.Equal("Mahagandhayon", inviteDto.MonasterySpaceName);
        Assert.Equal(2, inviteDto.InvitedById);
        Assert.Equal("Monk U Thawbana", inviteDto.InvitedByName);
        Assert.Equal(3, inviteDto.InvitedUserId);
        Assert.Equal("Ko Mg Mg", inviteDto.InvitedUserName);
        Assert.Equal(SpaceRoles.EditorId, inviteDto.SpaceRoleId);
        Assert.Equal(SpaceRoles.Editor, inviteDto.SpaceRoleName);
        Assert.Equal(InvitationStatus.Pending, inviteDto.Status);
    }

    [Fact]
    public async Task UserController_GetUserInvitationList_AllowsSelfAndAdmin_ForbidsOthers()
    {
        await using var db = CreateDbContext();
        var userService = new UserService(db);
        var controller = new UserController(userService);

        // 1. User 3 viewing their own invitations -> OK (200)
        var userClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "3"),
            new(ClaimTypes.Role, "User"),
            new("permission", Permissions.Invitation.View)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "test")) }
        };

        var ownResult = await controller.GetUserInvitationList(3, new());
        Assert.IsType<OkObjectResult>(ownResult);

        // 2. User 3 viewing User 4's invitations -> Forbid (403)
        var otherResult = await controller.GetUserInvitationList(4, new());
        Assert.IsType<ForbidResult>(otherResult);

        // 3. Admin viewing User 4's invitations -> OK (200)
        var adminClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, "Admin"),
            new("permission", Permissions.Users.View)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "test")) }
        };

        var adminResult = await controller.GetUserInvitationList(4, new());
        Assert.IsType<OkObjectResult>(adminResult);
    }

    [Fact]
    public async Task UserController_DeleteMonkProfile_AllowsSelfAndAdmin_ForbidsOthers()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new InfrastructureUser
        {
            Id = 10,
            Name = "Monk U Thawbana",
            PhoneNumber = "0911111111",
            Password = "pw",
            RoleId = 2, // Monk
            IsDeleted = false
        });
        db.MonkProfiles.Add(new MonkProfile
        {
            Id = 1,
            UserId = 10,
            MonasteryName = "Mahagandhayon"
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);
        var controller = new UserController(userService);

        // 1. User 3 (unauthorized regular user) -> Forbid (403)
        var userClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "3"),
            new(ClaimTypes.Role, "User")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "test")) }
        };
        var otherResult = await controller.DeleteMonkProfile(10);
        Assert.IsType<ForbidResult>(otherResult);

        // 2. User 10 (the monk owner themselves) -> OK (200)
        var monkClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "10"),
            new(ClaimTypes.Role, "Monk")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(monkClaims, "test")) }
        };
        var ownResult = await controller.DeleteMonkProfile(10);
        Assert.IsType<OkObjectResult>(ownResult);

        // 3. Admin (Id = 1) -> OK (200) after adding profile back
        db.MonkProfiles.Add(new MonkProfile
        {
            Id = 2,
            UserId = 10,
            MonasteryName = "Mahagandhayon Re-added"
        });
        await db.SaveChangesAsync();

        var adminClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, "Admin")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "test")) }
        };
        var adminResult = await controller.DeleteMonkProfile(10);
        Assert.IsType<OkObjectResult>(adminResult);
    }

    [Fact]
    public async Task UserController_UpdateMonkProfile_WithNullBody_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var userService = new UserService(db);
        var controller = new UserController(userService);

        var monkClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "10"),
            new(ClaimTypes.Role, "Monk")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(monkClaims, "test")) }
        };

        var result = await controller.UpdateMonkProfile(10, null);
        Assert.IsType<BadRequestObjectResult>(result);
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
}
