using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Donation.DTOs;
using HsumChaint.Domain.Features.Donation.Services;
using HsumChaint.Domain.Features.Monastery.DTOs;
using HsumChaint.Domain.Features.Monastery.Services;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Domain.Features.User.DTOs;
using HsumChaint.Domain.Features.User.Services;
using HsumChaint.Shared;
using HsumChaint.Shared.Authorization;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HsumChaint.Tests;

public class CursorPaginationServiceTests
{
    private static AppDbContext Context()
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        RbacTestData.Seed(context);
        return context;
    }

    [Fact]
    public async Task MonasteriesFeed_ReturnsCorrectPagesAndNextCursor()
    {
        await using var db = Context();
        for (var id = 1; id <= 12; id++)
        {
            db.MonasterySpaces.Add(new MonasterySpace
            {
                Id = id,
                MonasteryName = $"Monastery {id}",
                Address = $"Address {id}",
                IsDeleted = false
            });
        }
        db.MonasterySpaces.Add(new MonasterySpace
        {
            Id = 13,
            MonasteryName = "Deleted Monastery",
            IsDeleted = true
        });
        await db.SaveChangesAsync();

        var service = new MonasteryService(db);

        // 1. First page: LastId is null, PageSize = 5
        var first = await service.GetMonasteriesFeed(new CursorPaginationRequest { LastId = null, PageSize = 5 });
        Assert.True(first.IsSuccess);
        Assert.Equal(5, first.Data.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, first.Data.Select(x => x.Id));
        Assert.True(first.Pagination.HasNextPage);
        Assert.Equal(5, first.Pagination.NextCursor);

        // 2. Second page: LastId = 5, PageSize = 5
        var second = await service.GetMonasteriesFeed(new CursorPaginationRequest { LastId = 5, PageSize = 5 });
        Assert.True(second.IsSuccess);
        Assert.Equal(5, second.Data.Count);
        Assert.Equal(new[] { 6, 7, 8, 9, 10 }, second.Data.Select(x => x.Id));
        Assert.True(second.Pagination.HasNextPage);
        Assert.Equal(10, second.Pagination.NextCursor);

        // 3. Final page: LastId = 10, PageSize = 5
        var third = await service.GetMonasteriesFeed(new CursorPaginationRequest { LastId = 10, PageSize = 5 });
        Assert.True(third.IsSuccess);
        Assert.Equal(2, third.Data.Count);
        Assert.Equal(new[] { 11, 12 }, third.Data.Select(x => x.Id));
        Assert.False(third.Pagination.HasNextPage);
        Assert.Null(third.Pagination.NextCursor);

        // 4. Past end: LastId = 12
        var empty = await service.GetMonasteriesFeed(new CursorPaginationRequest { LastId = 12, PageSize = 5 });
        Assert.True(empty.IsSuccess);
        Assert.Empty(empty.Data);
        Assert.False(empty.Pagination.HasNextPage);
        Assert.Null(empty.Pagination.NextCursor);
    }

    [Fact]
    public async Task SearchMonasteriesFeed_FiltersByKeywordAndPaginates()
    {
        await using var db = Context();
        for (var id = 1; id <= 6; id++)
        {
            db.MonasterySpaces.Add(new MonasterySpace
            {
                Id = id,
                MonasteryName = id % 2 == 0 ? $"Yangon Shwe {id}" : $"Mandalay Taung {id}",
                IsDeleted = false
            });
        }
        await db.SaveChangesAsync();

        var service = new MonasteryService(db);

        // Search for "Yangon" -> IDs 2, 4, 6
        var first = await service.SearchMonasteriesFeed(new SearchMonasteriesFeedRequestDto
        {
            Keyword = "Yangon",
            LastId = null,
            PageSize = 2
        });
        Assert.True(first.IsSuccess);
        Assert.Equal(2, first.Data.Count);
        Assert.Equal(new[] { 2, 4 }, first.Data.Select(x => x.Id));
        Assert.True(first.Pagination.HasNextPage);
        Assert.Equal(4, first.Pagination.NextCursor);

        var second = await service.SearchMonasteriesFeed(new SearchMonasteriesFeedRequestDto
        {
            Keyword = "Yangon",
            LastId = 4,
            PageSize = 2
        });
        Assert.True(second.IsSuccess);
        Assert.Single(second.Data);
        Assert.Equal(6, second.Data[0].Id);
        Assert.False(second.Pagination.HasNextPage);
        Assert.Null(second.Pagination.NextCursor);
    }

    [Fact]
    public async Task MyMonasteriesFeed_ReturnsOnlyMemberMonasteriesWithCursor()
    {
        await using var db = Context();
        for (var id = 1; id <= 6; id++)
        {
            db.MonasterySpaces.Add(new MonasterySpace { Id = id, MonasteryName = $"Monastery {id}", IsDeleted = false });
            if (id <= 4)
            {
                db.MonasteryMembers.Add(new MonasteryMember
                {
                    Id = id,
                    UserId = 1,
                    MonasterySpaceId = id,
                    SpaceRoleId = SpaceRoles.OwnerId,
                    IsOwner = true
                });
            }
        }
        await db.SaveChangesAsync();

        var service = new MonasteryService(db);

        var first = await service.GetMyMonasteriesFeed(1, new CursorPaginationRequest { LastId = null, PageSize = 2 });
        Assert.True(first.IsSuccess);
        Assert.Equal(2, first.Data.Count);
        Assert.Equal(new[] { 1, 2 }, first.Data.Select(x => x.Id));
        Assert.True(first.Pagination.HasNextPage);
        Assert.Equal(2, first.Pagination.NextCursor);

        var second = await service.GetMyMonasteriesFeed(1, new CursorPaginationRequest { LastId = 2, PageSize = 2 });
        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Data.Count);
        Assert.Equal(new[] { 3, 4 }, second.Data.Select(x => x.Id));
        Assert.False(second.Pagination.HasNextPage);
        Assert.Null(second.Pagination.NextCursor);
    }

    [Fact]
    public async Task DonationsFeed_FiltersAndPaginatesWithCursor()
    {
        await using var db = Context();
        db.MonasterySpaces.Add(new MonasterySpace { Id = 10, MonasteryName = "Space 10", IsDeleted = false });
        for (var id = 1; id <= 8; id++)
        {
            db.DonorLists.Add(new DonorList
            {
                Id = id,
                DonorId = 1,
                MonasterySpaceId = 10,
                StatusValue = DonationStatus.Accepted
            });
        }
        await db.SaveChangesAsync();

        var service = new DonationService(db, Mock.Of<IFirebaseNotificationProvider>());

        var first = await service.GetDonationsFeed(1, new DonationFeedQueryDto
        {
            LastId = null,
            PageSize = 3,
            Status = DonationStatus.Accepted
        });
        Assert.True(first.IsSuccess);
        Assert.Equal(3, first.Data.Count);
        Assert.Equal(new[] { 1, 2, 3 }, first.Data.Select(x => x.Id));
        Assert.True(first.Pagination.HasNextPage);
        Assert.Equal(3, first.Pagination.NextCursor);

        var second = await service.GetDonationsFeed(1, new DonationFeedQueryDto
        {
            LastId = 3,
            PageSize = 3,
            Status = DonationStatus.Accepted
        });
        Assert.True(second.IsSuccess);
        Assert.Equal(3, second.Data.Count);
        Assert.Equal(new[] { 4, 5, 6 }, second.Data.Select(x => x.Id));
        Assert.True(second.Pagination.HasNextPage);
        Assert.Equal(6, second.Pagination.NextCursor);

        var third = await service.GetDonationsFeed(1, new DonationFeedQueryDto
        {
            LastId = 6,
            PageSize = 3,
            Status = DonationStatus.Accepted
        });
        Assert.True(third.IsSuccess);
        Assert.Equal(2, third.Data.Count);
        Assert.Equal(new[] { 7, 8 }, third.Data.Select(x => x.Id));
        Assert.False(third.Pagination.HasNextPage);
        Assert.Null(third.Pagination.NextCursor);
    }

    [Fact]
    public async Task NotificationsFeed_ReturnsUserNotificationsWithCursor()
    {
        await using var db = Context();
        for (var id = 1; id <= 5; id++)
        {
            db.Notifications.Add(new Notification
            {
                Id = id,
                UserId = 1,
                Message = $"Noti {id}",
                IsDeleted = false
            });
        }
        db.Notifications.Add(new Notification
        {
            Id = 6,
            UserId = 1,
            Message = "Deleted Noti",
            IsDeleted = true
        });
        await db.SaveChangesAsync();

        var service = new UserService(db);

        var first = await service.GetUserNotificationFeed(new GetUserNotificationsFeedRequestDto
        {
            Id = 1,
            LastId = null,
            PageSize = 3
        });
        Assert.True(first.IsSuccess);
        Assert.Equal(3, first.Data.Count);
        Assert.Equal(new[] { 1, 2, 3 }, first.Data.Select(x => x.Id));
        Assert.True(first.Pagination.HasNextPage);
        Assert.Equal(3, first.Pagination.NextCursor);

        var second = await service.GetUserNotificationFeed(new GetUserNotificationsFeedRequestDto
        {
            Id = 1,
            LastId = 3,
            PageSize = 3
        });
        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Data.Count);
        Assert.Equal(new[] { 4, 5 }, second.Data.Select(x => x.Id));
        Assert.False(second.Pagination.HasNextPage);
        Assert.Null(second.Pagination.NextCursor);
    }
}
