using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class NewFeaturesTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        RbacTestData.Seed(context);
        return context;
    }

    [Fact]
    public async Task GetMonasteries_PublicDiscovery_ReturnsActiveMonasteriesWithSearchFilter()
    {
        await using var db = CreateDbContext();
        db.MonasterySpaces.AddRange(
            new MonasterySpace { Id = 1, MonasteryName = "Mahagandhayon", Address = "Amarapura", IsDeleted = false },
            new MonasterySpace { Id = 2, MonasteryName = "Kyauk Gu", Address = "Bagan", IsDeleted = false },
            new MonasterySpace { Id = 3, MonasteryName = "Deleted Space", Address = "Yangon", IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var service = new MonasteryService(db);

        // 1. All active monasteries (pure listing)
        var resultAll = await service.GetMonasteries(new GetMonasteriesRequestDto { PageNumber = 1, PageSize = 10 });
        Assert.True(resultAll.IsSuccess);
        Assert.Equal(2, resultAll.Data!.Count);

        // 2. Search by name
        var resultSearchName = await service.SearchMonasteries(new SearchMonasteriesRequestDto { Keyword = "Maha", PageNumber = 1, PageSize = 10 });
        Assert.True(resultSearchName.IsSuccess);
        Assert.Single(resultSearchName.Data!);
        Assert.Equal("Mahagandhayon", resultSearchName.Data[0].MonasteryName);

        // 3. Search by address
        var resultSearchAddress = await service.SearchMonasteries(new SearchMonasteriesRequestDto { Keyword = "Bagan", PageNumber = 1, PageSize = 10 });
        Assert.True(resultSearchAddress.IsSuccess);
        Assert.Single(resultSearchAddress.Data!);
        Assert.Equal("Kyauk Gu", resultSearchAddress.Data[0].MonasteryName);
    }

    [Fact]
    public async Task MonkProfile_GetAndUpdate_WorksForMonkUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 10,
            Name = "U Pandita",
            PhoneNumber = "09111",
            Password = "pw",
            RoleId = 2, // Monk
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);

        // 1. Get profile before creating - returns empty fields
        var getResult = await userService.GetMonkProfile(10, 10);
        Assert.True(getResult.IsSuccess);
        Assert.Null(getResult.Data!.MonasteryName);

        // 2. Update profile
        var updateRequest = new UpdateMonkProfileRequestDto
        {
            MonasteryName = "Panditarama",
            MonasteryAddress = "Shwe Taung Gon, Yangon"
        };
        var updateResult = await userService.UpdateMonkProfile(10, 10, updateRequest);
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Panditarama", updateResult.Data!.MonasteryName);
        Assert.Equal("Shwe Taung Gon, Yangon", updateResult.Data!.MonasteryAddress);

        // 3. Get profile again
        var getUpdated = await userService.GetMonkProfile(10, 10);
        Assert.True(getUpdated.IsSuccess);
        Assert.Equal("Panditarama", getUpdated.Data!.MonasteryName);

        // 4. Null fields are preserved during partial updates.
        var partialUpdate = new UpdateMonkProfileRequestDto { MonasteryName = "Updated name" };
        var partialResult = await userService.UpdateMonkProfile(10, 10, partialUpdate);
        Assert.Equal("Updated name", partialResult.Data!.MonasteryName);
        Assert.Equal("Shwe Taung Gon, Yangon", partialResult.Data.MonasteryAddress);

        var nullResult = await userService.UpdateMonkProfile(10, 10, new UpdateMonkProfileRequestDto { MonasteryAddress = null });
        Assert.Equal("Updated name", nullResult.Data!.MonasteryName);
        Assert.Equal("Shwe Taung Gon, Yangon", nullResult.Data.MonasteryAddress);
    }

    [Fact]
    public async Task MonkProfile_ForNonMonk_Fails()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 20,
            Name = "Ko Aung",
            PhoneNumber = "09222",
            Password = "pw",
            RoleId = 3, // Regular User / Donor
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);
        var result = await userService.GetMonkProfile(20, 20);
        Assert.False(result.IsSuccess);
        Assert.Contains("not a monk", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MonkProfile_Delete_WorksForMonkUser_AndRemovesFromDb()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 10,
            Name = "U Pandita",
            PhoneNumber = "09111",
            Password = "pw",
            RoleId = 2, // Monk
            IsDeleted = false
        });
        db.MonkProfiles.Add(new MonkProfile
        {
            Id = 1,
            UserId = 10,
            MonasteryName = "Panditarama",
            MonasteryAddress = "Shwe Taung Gon, Yangon"
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);

        // Delete profile
        var deleteResult = await userService.DeleteMonkProfile(10, 10);
        Assert.True(deleteResult.IsSuccess);
        Assert.Equal("Monk profile deleted successfully.", deleteResult.Message);

        // Verify hard-deleted from database
        Assert.Null(await db.MonkProfiles.FirstOrDefaultAsync(p => p.UserId == 10));

        // GetMonkProfile gracefully returns empty defaults after delete
        var getAfterDelete = await userService.GetMonkProfile(10, 10);
        Assert.True(getAfterDelete.IsSuccess);
        Assert.Equal(0, getAfterDelete.Data!.Id);
        Assert.Null(getAfterDelete.Data.MonasteryName);
        Assert.Null(getAfterDelete.Data.MonasteryAddress);
    }

    [Fact]
    public async Task MonkProfile_Delete_ProfileNotFound_Fails()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 10,
            Name = "U Pandita",
            PhoneNumber = "09111",
            Password = "pw",
            RoleId = 2, // Monk
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);
        var result = await userService.DeleteMonkProfile(10, 10);
        Assert.False(result.IsSuccess);
        Assert.Contains("Monk profile not found.", result.Message);
    }

    [Fact]
    public async Task MonkProfile_Delete_UserNotFound_Fails()
    {
        await using var db = CreateDbContext();
        var userService = new UserService(db);
        var result = await userService.DeleteMonkProfile(999, 999);
        Assert.False(result.IsSuccess);
        Assert.Contains("User not found.", result.Message);
    }

    [Fact]
    public async Task MonkProfile_Delete_ForNonMonk_Fails()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 20,
            Name = "Ko Aung",
            PhoneNumber = "09222",
            Password = "pw",
            RoleId = 3, // Regular User / Donor
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);
        var result = await userService.DeleteMonkProfile(20, 20);
        Assert.False(result.IsSuccess);
        Assert.Contains("User is not a monk.", result.Message);
    }

    [Fact]
    public async Task MonkProfile_Update_NullRequest_Fails()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 10,
            Name = "U Pandita",
            PhoneNumber = "09111",
            Password = "pw",
            RoleId = 2, // Monk
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);
        var result = await userService.UpdateMonkProfile(10, 10, null!);
        Assert.False(result.IsSuccess);
        Assert.Contains("Request data is required.", result.Message);
    }

    [Fact]
    public async Task UserSettings_GetDefaultsAndSave_Succeeds()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 30,
            Name = "Daw Hla",
            PhoneNumber = "09333",
            Password = "pw",
            RoleId = 3,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var userService = new UserService(db);

        // 1. Get default settings
        var defaults = await userService.GetUserSettings(30, 30);
        Assert.True(defaults.IsSuccess);
        Assert.NotNull(defaults.Data!.PickupNotificationTime);
        Assert.Equal(15, defaults.Data.PickupNotificationTime.Value.Hour);
        Assert.Equal(30, defaults.Data.PickupNotificationTime.Value.Minute);
        Assert.Equal(22, defaults.Data.DropoffNotificationTime!.Value.Hour);

        // 2. Update settings
        var customPickupNoti = DateTime.UtcNow.Date.AddHours(14);
        var customDropoffNoti = DateTime.UtcNow.Date.AddHours(21);
        var updateResult = await userService.UpdateUserSettings(30, 30, new UpdateUserSettingRequestDto
        {
            PickupNotificationTime = customPickupNoti,
            DropoffNotificationTime = customDropoffNoti
        });
        Assert.True(updateResult.IsSuccess);
        Assert.Equal(14, updateResult.Data!.PickupNotificationTime!.Value.Hour);
        Assert.Equal(21, updateResult.Data!.DropoffNotificationTime!.Value.Hour);
    }

    [Fact]
    public async Task BatchDonations_CreateManual_InsertsMultipleRecordsAtomically()
    {
        await using var db = CreateDbContext();
        var mockNotification = new Mock<IFirebaseNotificationProvider>();

        db.Users.Add(new User { Id = 1, Name = "Monk Owner", PhoneNumber = "091", Password = "pw", RoleId = 2, IsDeleted = false });
        db.MonasterySpaces.Add(new MonasterySpace { Id = 100, MonasteryName = "Test Space", CreatedById = 1 });
        db.MonasteryMembers.Add(new MonasteryMember { UserId = 1, MonasterySpaceId = 100, SpaceRoleId = SpaceRoles.OwnerId, IsOwner = true });
        await db.SaveChangesAsync();

        var donationService = new DonationService(db, mockNotification.Object);

        var request = new CreateBatchDonationRequestDto
        {
            MonasterySpaceId = 100,
            Donations = new List<CreateManualDonationRequestDto>
            {
                new() { DonorName = "Donor A", DonationType = DonationType.Food, Amount = 10000, Note = "First" },
                new() { DonorName = "Donor B", DonationType = DonationType.Money, Amount = 20000, Note = "Second" },
                new() { DonorName = "Donor C", DonationType = DonationType.Supplies, Amount = 30000, Note = "Third" }
            }
        };

        var result = await donationService.CreateBatchManualDonations(1, request);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.Count);
        Assert.Equal(3, await db.DonorLists.CountAsync(d => d.MonasterySpaceId == 100));
    }

    [Fact]
    public async Task RecurringDonations_CreatesCorrectMonthlySeries()
    {
        await using var db = CreateDbContext();
        var mockNotification = new Mock<IFirebaseNotificationProvider>();

        db.Users.Add(new User { Id = 5, Name = "Devotee U Ba", PhoneNumber = "09555", Password = "pw", RoleId = 3, IsDeleted = false });
        db.MonasterySpaces.Add(new MonasterySpace { Id = 200, MonasteryName = "Forest Monastery", CreatedById = 1 });
        await db.SaveChangesAsync();

        var donationService = new DonationService(db, mockNotification.Object);

        var request = new CreateRecurringDonationRequestDto
        {
            MonasterySpaceId = 200,
            DonationType = DonationType.Food,
            Amount = 50000,
            StartDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            DayOfMonth = 15,
            MonthsCount = 6,
            PickupTimeOfDay = new TimeSpan(6, 0, 0),
            Note = "Monthly Alms"
        };

        var result = await donationService.RequestRecurringDonation(5, request);
        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Data!.Count);
        Assert.Equal(6, await db.DonorLists.CountAsync(d => d.MonasterySpaceId == 200));

        // Check each month is sequential
        for (int i = 0; i < 6; i++)
        {
            var targetMonth = 1 + i;
            var item = result.Data[i];
            Assert.Contains("[Series:", item.Note);
            Assert.Equal(targetMonth, item.PickupTime!.Value.Month);
            Assert.Equal(15, item.PickupTime.Value.Day);
            Assert.Equal(6, item.PickupTime.Value.Hour);
        }
    }

    [Fact]
    public async Task DeleteDonation_ActiveDonation_DeletesSuccessfully()
    {
        await using var db = CreateDbContext();
        var mockNotification = new Mock<IFirebaseNotificationProvider>();

        db.Users.Add(new User { Id = 1, Name = "Monk Owner", PhoneNumber = "091", Password = "pw", RoleId = 2, IsDeleted = false });
        db.MonasterySpaces.Add(new MonasterySpace { Id = 300, MonasteryName = "Space 300", CreatedById = 1 });
        db.MonasteryMembers.Add(new MonasteryMember { UserId = 1, MonasterySpaceId = 300, SpaceRoleId = SpaceRoles.OwnerId, IsOwner = true });

        var donation = new DonorList
        {
            Id = 50,
            MonasterySpaceId = 300,
            DonorName = "Mistaken Donor",
            DonationType = "Food",
            Status = "PendingReview",
            StatusValue = DonationStatus.PendingReview
        };
        db.DonorLists.Add(donation);
        await db.SaveChangesAsync();

        var donationService = new DonationService(db, mockNotification.Object);

        var deleteResult = await donationService.DeleteDonation(1, 50);
        Assert.True(deleteResult.IsSuccess);
        Assert.Null(await db.DonorLists.FindAsync(50));
    }

    [Fact]
    public async Task DeleteDonation_CompletedDonation_RejectsDeletion()
    {
        await using var db = CreateDbContext();
        var mockNotification = new Mock<IFirebaseNotificationProvider>();

        db.Users.Add(new User { Id = 1, Name = "Monk Owner", PhoneNumber = "091", Password = "pw", RoleId = 2, IsDeleted = false });
        db.MonasterySpaces.Add(new MonasterySpace { Id = 400, MonasteryName = "Space 400", CreatedById = 1 });
        db.MonasteryMembers.Add(new MonasteryMember { UserId = 1, MonasterySpaceId = 400, SpaceRoleId = SpaceRoles.OwnerId, IsOwner = true });

        var donation = new DonorList
        {
            Id = 55,
            MonasterySpaceId = 400,
            DonorName = "Completed Donor",
            DonationType = "Food",
            Status = "Completed",
            StatusValue = DonationStatus.Completed
        };
        db.DonorLists.Add(donation);
        await db.SaveChangesAsync();

        var donationService = new DonationService(db, mockNotification.Object);

        var deleteResult = await donationService.DeleteDonation(1, 55);
        Assert.False(deleteResult.IsSuccess);
        Assert.Contains("Completed donations cannot be deleted", deleteResult.Message);
        Assert.NotNull(await db.DonorLists.FindAsync(55));
    }

    [Fact]
    public async Task MonthlyCalendar_ReturnsGroupedDaysAndTotals()
    {
        await using var db = CreateDbContext();
        var mockNotification = new Mock<IFirebaseNotificationProvider>();

        db.Users.Add(new User { Id = 1, Name = "Monk Owner", PhoneNumber = "091", Password = "pw", RoleId = 2, IsDeleted = false });
        db.MonasterySpaces.Add(new MonasterySpace { Id = 500, MonasteryName = "Space 500", CreatedById = 1 });
        db.MonasteryMembers.Add(new MonasteryMember { UserId = 1, MonasterySpaceId = 500, SpaceRoleId = SpaceRoles.OwnerId, IsOwner = true });

        // Add donations for November 2026: two on Nov 5, one on Nov 10, one in December (should be excluded)
        db.DonorLists.AddRange(
            new DonorList
            {
                Id = 1,
                MonasterySpaceId = 500,
                DonorName = "Donor 1",
                DonationType = "Food",
                Amount = 10000,
                PickupTime = new DateTime(2026, 11, 5, 6, 0, 0, DateTimeKind.Utc),
                StatusValue = DonationStatus.Scheduled
            },
            new DonorList
            {
                Id = 2,
                MonasterySpaceId = 500,
                DonorName = "Donor 2",
                DonationType = "Cash",
                Amount = 25000,
                PickupTime = new DateTime(2026, 11, 5, 7, 0, 0, DateTimeKind.Utc),
                StatusValue = DonationStatus.Accepted
            },
            new DonorList
            {
                Id = 3,
                MonasterySpaceId = 500,
                DonorName = "Donor 3",
                DonationType = "Utilities",
                Amount = 15000,
                PickupTime = new DateTime(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc),
                StatusValue = DonationStatus.Completed
            },
            new DonorList
            {
                Id = 4,
                MonasterySpaceId = 500,
                DonorName = "Dec Donor",
                DonationType = "Food",
                Amount = 50000,
                PickupTime = new DateTime(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc),
                StatusValue = DonationStatus.Accepted
            }
        );
        await db.SaveChangesAsync();

        var donationService = new DonationService(db, mockNotification.Object);

        var query = new MonthlyCalendarQueryDto
        {
            MonasterySpaceId = 500,
            Year = 2026,
            Month = 11
        };

        var result = await donationService.GetMonthlyCalendar(1, query);
        Assert.True(result.IsSuccess);
        Assert.Equal(2026, result.Data!.Year);
        Assert.Equal(11, result.Data.Month);
        Assert.Equal(3, result.Data.TotalDonationsCount); // Dec item excluded!
        Assert.Equal(50000, result.Data.TotalAmount); // 10k + 25k + 15k
        Assert.Equal(2, result.Data.Days.Count); // Day 5 and Day 10

        var day5 = result.Data.Days.First(d => d.Day == 5);
        Assert.Equal(2, day5.DonationsCount);
        Assert.Equal("2026-11-05", day5.Date);

        var day10 = result.Data.Days.First(d => d.Day == 10);
        Assert.Equal(1, day10.DonationsCount);
        Assert.Equal("2026-11-10", day10.Date);
    }

    [Fact]
    public async Task GetDonations_OrdersAscendingByDefault()
    {
        await using var db = CreateDbContext();
        var mockNotification = new Mock<IFirebaseNotificationProvider>();

        db.Users.Add(new User { Id = 1, Name = "Monk Owner", PhoneNumber = "091", Password = "pw", RoleId = 2, IsDeleted = false });
        db.MonasterySpaces.Add(new MonasterySpace { Id = 600, MonasteryName = "Space 600", CreatedById = 1 });
        db.MonasteryMembers.Add(new MonasteryMember { UserId = 1, MonasterySpaceId = 600, SpaceRoleId = SpaceRoles.OwnerId, IsOwner = true });

        db.DonorLists.AddRange(
            new DonorList { Id = 1, MonasterySpaceId = 600, DonorName = "Earlier", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DonorList { Id = 2, MonasterySpaceId = 600, DonorName = "Later", CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) }
        );
        await db.SaveChangesAsync();

        var donationService = new DonationService(db, mockNotification.Object);
        var result = await donationService.GetDonations(1, new DonationQueryDto { MonasterySpaceId = 600, PageNumber = 1, PageSize = 10 });
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal(1, result.Data[0].Id);
        Assert.Equal(2, result.Data[1].Id);
    }
}
