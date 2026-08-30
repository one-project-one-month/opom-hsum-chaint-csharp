using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Donation.DTOs;
using HsumChaint.Domain.Features.Donation.Services;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HsumChaint.Tests;

public class DonationServiceTests
{
    [Fact]
    public async Task RequestDonation_CreatesPendingDonation_AndNotifiesAdmins()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Admin", "091");
        SeedUser(dbContext, 2, "Donor", "092");
        SeedMonastery(dbContext, 10, 1);
        await dbContext.SaveChangesAsync();
        var notificationProvider = new Mock<IFirebaseNotificationProvider>();
        var service = new DonationService(dbContext, notificationProvider.Object);

        var response = await service.RequestDonation(2, new CreateDonationRequestDto
        {
            MonasterySpaceId = 10,
            DonationType = DonationType.Food,
            Quantity = 12,
            Note = "Rice bags"
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(DonationStatus.PendingReview, response.Data!.Status);
        Assert.Equal(2, response.Data.DonorId);
        Assert.Single(dbContext.DonorLists);
        Assert.Single(dbContext.Notifications);
        Assert.Equal(1, dbContext.Notifications.Single().UserId);
    }

    [Fact]
    public async Task CreateManualDonation_AllowsAdmin_AndRecordsReviewer()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Admin", "091");
        SeedUser(dbContext, 2, "Donor", "092");
        SeedMonastery(dbContext, 10, 1);
        await dbContext.SaveChangesAsync();
        var service = new DonationService(dbContext, Mock.Of<IFirebaseNotificationProvider>());

        var response = await service.CreateManualDonation(1, new CreateManualDonationRequestDto
        {
            MonasterySpaceId = 10,
            DonorId = 2,
            DonationType = DonationType.Money,
            Amount = 50000
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(DonationStatus.Accepted, response.Data!.Status);
        Assert.Equal(1, response.Data.ReviewerId);
        Assert.NotNull(response.Data.ReviewedAt);
    }

    [Fact]
    public async Task CreateManualDonation_RejectsViewer()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Owner", "091");
        SeedUser(dbContext, 2, "Viewer", "092");
        SeedMonastery(dbContext, 10, 1);
        dbContext.MonasteryMembers.Add(new MonasteryMember
        {
            UserId = 2,
            MonasterySpaceId = 10,
            Role = MonasteryRole.Viewer,
            IsOwner = false
        });
        await dbContext.SaveChangesAsync();
        var service = new DonationService(dbContext, Mock.Of<IFirebaseNotificationProvider>());

        var response = await service.CreateManualDonation(2, new CreateManualDonationRequestDto
        {
            MonasterySpaceId = 10,
            DonorName = "Cash donor",
            DonationType = DonationType.Money,
            Amount = 10000
        });

        Assert.False(response.IsSuccess);
        Assert.Contains("not authorized", response.Message);
    }

    [Fact]
    public async Task ReviewDonation_AcceptsPendingDonation_AndNotifiesDonor()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Admin", "091");
        SeedUser(dbContext, 2, "Donor", "092", "fcm-token");
        SeedMonastery(dbContext, 10, 1);
        SeedDonation(dbContext, 100, 10, 2, DonationStatus.PendingReview);
        await dbContext.SaveChangesAsync();
        var notificationProvider = new Mock<IFirebaseNotificationProvider>();
        var service = new DonationService(dbContext, notificationProvider.Object);

        var response = await service.ReviewDonation(1, 100, new ReviewDonationRequestDto
        {
            Status = DonationStatus.Accepted,
            Note = "Accepted"
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(DonationStatus.Accepted, response.Data!.Status);
        Assert.Equal(1, response.Data.ReviewerId);
        Assert.Single(dbContext.Notifications);
        notificationProvider.Verify(x => x.SendPushAsync(
            "fcm-token",
            "Donation Update",
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleDonation_AllowsEditor()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Owner", "091");
        SeedUser(dbContext, 2, "Editor", "092");
        SeedUser(dbContext, 3, "Donor", "093");
        SeedMonastery(dbContext, 10, 1);
        dbContext.MonasteryMembers.Add(new MonasteryMember
        {
            UserId = 2,
            MonasterySpaceId = 10,
            Role = MonasteryRole.Editor,
            IsOwner = false
        });
        SeedDonation(dbContext, 100, 10, 3, DonationStatus.Accepted);
        await dbContext.SaveChangesAsync();
        var service = new DonationService(dbContext, Mock.Of<IFirebaseNotificationProvider>());
        var pickupTime = DateTime.UtcNow.AddDays(1);

        var response = await service.ScheduleDonation(2, 100, new ScheduleDonationRequestDto
        {
            PickupTime = pickupTime
        });

        Assert.True(response.IsSuccess);
        Assert.Equal(DonationStatus.Scheduled, response.Data!.Status);
        Assert.Equal(pickupTime, response.Data.PickupTime);
    }

    [Fact]
    public async Task CompleteDonation_MarksDonationCompleted()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 1, "Admin", "091");
        SeedUser(dbContext, 2, "Donor", "092");
        SeedMonastery(dbContext, 10, 1);
        SeedDonation(dbContext, 100, 10, 2, DonationStatus.Scheduled);
        await dbContext.SaveChangesAsync();
        var service = new DonationService(dbContext, Mock.Of<IFirebaseNotificationProvider>());

        var response = await service.CompleteDonation(1, 100);

        Assert.True(response.IsSuccess);
        Assert.Equal(DonationStatus.Completed, response.Data!.Status);
        Assert.NotNull(response.Data.CompletedAt);
    }

    [Fact]
    public async Task CancelDonation_AllowsDonorToCancelOwnDonation()
    {
        await using var dbContext = CreateDbContext();
        SeedUser(dbContext, 2, "Donor", "092");
        dbContext.MonasterySpaces.Add(new MonasterySpace { Id = 10, MonasteryName = "Aung Myae" });
        SeedDonation(dbContext, 100, 10, 2, DonationStatus.PendingReview);
        await dbContext.SaveChangesAsync();
        var service = new DonationService(dbContext, Mock.Of<IFirebaseNotificationProvider>());

        var response = await service.CancelDonation(2, 100);

        Assert.True(response.IsSuccess);
        Assert.Equal(DonationStatus.Cancelled, response.Data!.Status);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedUser(AppDbContext dbContext, int id, string name, string phone, string? fcmToken = null)
    {
        dbContext.Users.Add(new HsumChaint.Database.Models.User
        {
            Id = id,
            Name = name,
            PhoneNumber = phone,
            Password = "pw",
            UserType = UserType.User,
            FcmToken = fcmToken,
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

    private static void SeedDonation(AppDbContext dbContext, int donationId, int monasteryId, int donorId, DonationStatus status)
    {
        dbContext.DonorLists.Add(new DonorList
        {
            Id = donationId,
            MonasterySpaceId = monasteryId,
            DonorId = donorId,
            DonorName = "Donor",
            DonationType = DonationType.Food.ToString(),
            DonationTypeValue = DonationType.Food,
            Quantity = 1,
            Status = status.ToString(),
            StatusValue = status,
            CreatedAt = DateTime.UtcNow
        });
    }
}
