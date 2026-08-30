using AutoMapper;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Notification.DTOs;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Domain.Features.Notification.Services;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using InfrastructureNotification = HsumChaint.Database.Models.Notification;

namespace HsumChaint.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendNotificationAndStore_ReturnsSuccess_WhenNotificationCanBeCreated()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new HsumChaint.Database.Models.User
        {
            Id = 1,
            Name = "Donor",
            PhoneNumber = "091111111",
            Password = "pw",
            UserType = UserType.User,
            IsDeleted = false
        });
        await dbContext.SaveChangesAsync();

        var notificationProvider = new Mock<IFirebaseNotificationProvider>();
        var notificationService = new NotificationServices(dbContext, notificationProvider.Object, CreateMapper().Object);

        var response = await notificationService.SendNotificationAndStore(new CreateNotificationRequestDto
        {
            UserId = 1,
            NotificationType = "donation",
            Message = "A donation was made"
        });

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data!.UserId);
        Assert.Single(dbContext.Notifications);
        notificationProvider.Verify(x => x.SendPushAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ReadNotification_ReturnsSuccess_WhenNotificationExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.Add(new InfrastructureNotification
        {
            Id = 5,
            UserId = 7,
            IsRead = false,
            IsDelete = false,
            Type = NotificationType.Donation
        });
        await dbContext.SaveChangesAsync();
        var notificationService = new NotificationServices(dbContext, Mock.Of<IFirebaseNotificationProvider>(), CreateMapper().Object);

        var response = await notificationService.ReadNotification(new ReadNotificationRequestDto { UserId = 7, NotificationId = 5 });

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(5, response.Data!.NotificationId);
        Assert.True((await dbContext.Notifications.FindAsync(5))!.IsRead);
    }

    [Fact]
    public async Task DeleteNotification_ReturnsSuccess_WhenNotificationCanBeDeleted()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.Add(new InfrastructureNotification
        {
            Id = 9,
            UserId = 7,
            IsRead = false,
            IsDelete = false,
            Type = NotificationType.Donation
        });
        await dbContext.SaveChangesAsync();
        var notificationService = new NotificationServices(dbContext, Mock.Of<IFirebaseNotificationProvider>(), CreateMapper().Object);

        var response = await notificationService.DeleteNotification(new DeleteNotificationRequestDto { UserId = 7, NotificationId = 9 });

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(9, response.Data!.NotificationId);
        Assert.True((await dbContext.Notifications.FindAsync(9))!.IsDelete);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Mock<IMapper> CreateMapper()
    {
        var mapper = new Mock<IMapper>();

        mapper
            .Setup(x => x.Map<InfrastructureNotification>(It.IsAny<CreateNotificationRequestDto>()))
            .Returns((CreateNotificationRequestDto request) => new InfrastructureNotification
            {
                UserId = request.UserId,
                Type = ResolveNotificationType(request.NotificationType),
                Message = request.Message,
                IsRead = false,
                IsDelete = false
            });

        mapper
            .Setup(x => x.Map<CreateNotificationResponseDto>(It.IsAny<InfrastructureNotification>()))
            .Returns((InfrastructureNotification notification) => new CreateNotificationResponseDto
            {
                UserId = notification.UserId ?? 0,
                NotificationId = notification.Id,
                NotificationType = notification.Type.ToString(),
                Message = notification.Message
            });

        mapper
            .Setup(x => x.Map<ReadNotificationResponseDto>(It.IsAny<InfrastructureNotification>()))
            .Returns((InfrastructureNotification notification) => new ReadNotificationResponseDto
            {
                NotificationId = notification.Id,
                IsRead = notification.IsRead ?? false
            });

        mapper
            .Setup(x => x.Map<DeleteNotificationResponseDto>(It.IsAny<InfrastructureNotification>()))
            .Returns((InfrastructureNotification notification) => new DeleteNotificationResponseDto
            {
                NotificationId = notification.Id,
                IsDeleted = notification.IsDelete ?? false
            });

        return mapper;
    }

    private static NotificationType ResolveNotificationType(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.TryParse<NotificationType>(value, ignoreCase: true, out var parsedType))
        {
            return parsedType;
        }

        return NotificationType.System;
    }
}
