using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Donation.Services;
using HsumChaint.Domain.Features.Monastery.Services;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Domain.Features.RolePermission.Services;
using HsumChaint.Domain.Features.User.Services;
using HsumChaint.Shared;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HsumChaint.Tests;

public class PaginationServiceTests
{
    private static AppDbContext Context()
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        RbacTestData.Seed(context);
        return context;
    }

    [Fact]
    public async Task Users_PageInIdOrderAndExcludeDeletedUsers()
    {
        await using var db = Context();
        for (var id = 12; id >= 1; id--)
            db.Users.Add(new User { Id = id, Name = $"User {id}", PhoneNumber = $"09{id}", Password = "pw", RoleId = 3, IsDeleted = false });
        db.Users.Add(new User { Id = 13, Name = "Deleted", PhoneNumber = "0913", Password = "pw", RoleId = 3, IsDeleted = true });
        await db.SaveChangesAsync();
        var service = new UserService(db);
        var first = await service.GetAllUsers();
        Assert.Equal(Enumerable.Range(1, 10), first.Data.Select(u => u.Id));
        Assert.Equal(12, first.Pagination.TotalCount);
        Assert.True(first.Pagination.HasNextPage);
        var second = await service.GetAllUsers(new() { PageNumber = 2 });
        Assert.Equal(new[] { 11, 12 }, second.Data.Select(u => u.Id));
        Assert.False(second.Pagination.HasNextPage);
        var pastEnd = await service.GetAllUsers(new() { PageNumber = 3 });
        Assert.True(pastEnd.IsSuccess);
        Assert.Empty(pastEnd.Data);
        Assert.Equal(12, pastEnd.Pagination.TotalCount);
    }

    [Fact]
    public async Task InvitationsAndNotifications_CountOnlyMatchingRecordsBeforePaging()
    {
        await using var db = Context();
        for (var id = 1; id <= 12; id++)
        {
            db.Invitations.Add(new Invitation { Id = id, InvitedUserId = 1, InvitedById = 2, RoleId = 3 });
            db.Notifications.Add(new Notification { Id = id, UserId = 1, IsDelete = false });
        }
        db.Invitations.Add(new Invitation { Id = 13, InvitedUserId = 9, InvitedById = 9, RoleId = 3 });
        db.Notifications.Add(new Notification { Id = 13, UserId = 9, IsDelete = false });
        db.Notifications.Add(new Notification { Id = 14, UserId = 1, IsDelete = true });
        await db.SaveChangesAsync();
        var service = new UserService(db);
        var page = new PaginationRequest { PageNumber = 2 };
        var invited = await service.GetUserInvitationList(1, page);
        var sent = await service.GetInvitedByOtherList(2, page);
        var notifications = await service.GetUserNotificationList(1, page);
        Assert.Equal(new[] { 11, 12 }, invited.Data.Select(i => i.Id));
        Assert.Equal(new[] { 11, 12 }, sent.Data.Select(i => i.Id));
        Assert.Equal(new[] { 11, 12 }, notifications.Data.Select(n => n.Id));
        Assert.Equal(12, invited.Pagination.TotalCount);
        Assert.Equal(12, sent.Pagination.TotalCount);
        Assert.Equal(12, notifications.Pagination.TotalCount);
    }

    [Fact]
    public async Task Donations_FilterBeforeCountingAndUseIdToBreakDateTies()
    {
        await using var db = Context();
        var date = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);
        for (var id = 1; id <= 12; id++)
            db.DonorLists.Add(new DonorList { Id = id, DonorId = 1, MonasterySpaceId = 10, CreatedAt = date, StatusValue = DonationStatus.Accepted });
        db.DonorLists.Add(new DonorList { Id = 13, DonorId = 2, MonasterySpaceId = 10, CreatedAt = date, StatusValue = DonationStatus.Accepted });
        db.DonorLists.Add(new DonorList { Id = 14, DonorId = 1, MonasterySpaceId = 10, CreatedAt = date, StatusValue = DonationStatus.Rejected });
        await db.SaveChangesAsync();
        var service = new DonationService(db, Mock.Of<IFirebaseNotificationProvider>());
        var result = await service.GetDonations(1, new() { PageNumber = 2, PageSize = 5, Status = DonationStatus.Accepted, FromDate = date, ToDate = date });
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 7, 6, 5, 4, 3 }, result.Data.Select(d => d.Id));
        Assert.Equal(12, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.True((await service.GetDonations(1, new() { MonasterySpaceId = 10 })).IsFailure);
        Assert.True((await service.GetDonations(1, new() { DonorId = 2 })).IsFailure);
    }

    [Fact]
    public async Task MonasteriesAndMembers_KeepMembershipScopeWhenPaging()
    {
        await using var db = Context();
        for (var id = 1; id <= 12; id++)
        {
            db.Users.Add(new User { Id = id, Name = $"User {id}", PhoneNumber = $"09{id}", Password = "pw", RoleId = 3, IsDeleted = false });
            db.MonasterySpaces.Add(new MonasterySpace { Id = id, MonasteryName = $"Monastery {id}" });
            db.MonasteryMembers.Add(new MonasteryMember { Id = id, UserId = 1, MonasterySpaceId = id, RoleId = 3 });
        }
        for (var id = 2; id <= 12; id++)
            db.MonasteryMembers.Add(new MonasteryMember { Id = id + 12, UserId = id, MonasterySpaceId = 1, RoleId = 3 });
        db.MonasterySpaces.Add(new MonasterySpace { Id = 13, MonasteryName = "Other" });
        await db.SaveChangesAsync();
        var service = new MonasteryService(db);
        var monasteries = await service.GetMyMonasteries(1, new() { PageNumber = 2 });
        Assert.Equal(new[] { 11, 12 }, monasteries.Data.Select(m => m.Id));
        Assert.Equal(12, monasteries.Pagination.TotalCount);
        var members = await service.GetMembers(1, 1, new() { PageNumber = 2 });
        Assert.Equal(new[] { 11, 12 }, members.Data.Select(m => m.UserId));
        Assert.Equal(12, members.Pagination.TotalCount);
        Assert.True((await service.GetMembers(2, 13)).IsFailure);
        Assert.Empty((await service.GetMyMonasteries(99)).Data);
    }

    [Fact]
    public async Task RolesAndPermissions_PageAndExcludeInactiveRecords()
    {
        await using var db = Context();
        db.Permissions.Find(1)!.IsDeleted = true;
        db.Roles.Find(4)!.IsDeleted = true;
        await db.SaveChangesAsync();
        var service = new RolePermissionService(db);
        var roles = await service.GetRoles(new() { PageSize = 2 });
        Assert.Equal(2, roles.Data.Count);
        Assert.Equal(4, roles.Pagination.TotalCount);
        Assert.DoesNotContain(roles.Data, r => r.Id == 4);
        var first = await service.GetPermissions();
        var second = await service.GetPermissions(new() { PageNumber = 2 });
        Assert.Equal(10, first.Data.Count);
        Assert.Equal(4, second.Data.Count);
        Assert.Equal(14, first.Pagination.TotalCount);
        Assert.Empty(first.Data.Select(p => p.Id).Intersect(second.Data.Select(p => p.Id)));
        Assert.DoesNotContain(first.Data.Concat(second.Data), p => p.Id == 1);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    [InlineData(1, -1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public async Task AllCollections_RejectInvalidPagination(int number, int size)
    {
        await using var db = Context();
        var request = new PaginationRequest { PageNumber = number, PageSize = size };
        var users = new UserService(db);
        var monasteries = new MonasteryService(db);
        var roles = new RolePermissionService(db);
        var donations = new DonationService(db, Mock.Of<IFirebaseNotificationProvider>());
        Assert.True((await users.GetAllUsers(request)).IsFailure);
        Assert.True((await users.GetUserInvitationList(1, request)).IsFailure);
        Assert.True((await users.GetInvitedByOtherList(1, request)).IsFailure);
        Assert.True((await users.GetUserNotificationList(1, request)).IsFailure);
        Assert.True((await monasteries.GetMyMonasteries(1, request)).IsFailure);
        Assert.True((await monasteries.GetMembers(1, 1, request)).IsFailure);
        Assert.True((await roles.GetRoles(request)).IsFailure);
        Assert.True((await roles.GetPermissions(request)).IsFailure);
        Assert.True((await donations.GetDonations(1, new() { PageNumber = number, PageSize = size })).IsFailure);
    }
}
