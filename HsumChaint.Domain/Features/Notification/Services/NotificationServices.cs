using HsumChaint.Shared;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Notification.DTOs;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Domain.Features.Notification.ServiceInterfaces;
using HsumChaint.Shared.CommonEnum;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = HsumChaint.Database.Models.Notification;

namespace HsumChaint.Domain.Features.Notification.Services
{
    public class NotificationServices : INotificationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IFirebaseNotificationProvider _notificationProvider;

        public NotificationServices(AppDbContext dbContext, IFirebaseNotificationProvider notificationProvider)
        {
            _dbContext = dbContext;
            _notificationProvider = notificationProvider;
        }

        public async Task<Result<DeleteNotificationResponseDto>> DeleteNotification(DeleteNotificationRequestDto requestModel)
        {
            try
            {
                #region Fetch and Validate Notification
                var notification = await _dbContext.Notifications.FindAsync(requestModel.NotificationId);

                if (notification == null)
                {
                    return Result<DeleteNotificationResponseDto>.Failure("Notification not found.");
                }

                if (notification.UserId != requestModel.UserId)
                {
                    return Result<DeleteNotificationResponseDto>.Failure("Unauthorized access to this notification.");
                }
                #endregion

                #region Update Database
                if (notification.IsDelete == true)
                {
                    return Result<DeleteNotificationResponseDto>.Success(MapDeleteResponse(notification), "Notification is already deleted.");
                }

                notification.IsDelete = true;

                _dbContext.Notifications.Update(notification);
                await _dbContext.SaveChangesAsync();

                return Result<DeleteNotificationResponseDto>.Success(MapDeleteResponse(notification), "Notification deleted successfully.");
                #endregion
            }
            catch (Exception ex)
            {
                return Result<DeleteNotificationResponseDto>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }

        public async Task<Result<CreateNotificationResponseDto>> SendNotificationAndStore(CreateNotificationRequestDto requestModel)
        {
            try
            {
                #region Check If User Exists
                var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == requestModel.UserId && x.IsDeleted == false);
                if (user == null)
                {
                    return Result<CreateNotificationResponseDto>.Failure("User not found");
                }
                #endregion

                #region Get FCM token and Store Notification into DB
                var deviceFcmToken = user.FcmToken;

                var notification = MapToEntity(requestModel);

                await _dbContext.Notifications.AddAsync(notification);
                await _dbContext.SaveChangesAsync();

                #endregion

                #region Send Notification
                if (!string.IsNullOrWhiteSpace(deviceFcmToken))
                {
                    var title = GetTitleForType(requestModel.NotificationType);

                    var payloadData = new Dictionary<string, string>
                    {
                        { "notificationId", notification.Id.ToString() },
                        {"type", requestModel.NotificationType ?? "" }
                    };

                    await _notificationProvider.SendPushAsync(deviceFcmToken, title, requestModel.Message, payloadData);
                }
                #endregion
                return Result<CreateNotificationResponseDto>.Success(MapCreateResponse(notification), "Notification added successfully.");
            }
            catch (Exception ex)
            {
                return Result<CreateNotificationResponseDto>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }

        public async Task<Result<ReadNotificationResponseDto>> ReadNotification(ReadNotificationRequestDto requestModel)
        {
            try
            {
                #region Fetch and Validate Notification
                var notification = await _dbContext.Notifications.FindAsync(requestModel.NotificationId);

                if (notification == null)
                {
                    return Result<ReadNotificationResponseDto>.Failure("Notification not found.");
                }

                if (notification.UserId != requestModel.UserId)
                {
                    return Result<ReadNotificationResponseDto>.Failure("Unauthorized access to this notification.");
                }
                #endregion

                #region Update Database
                if (notification.IsRead == true)
                {
                    return Result<ReadNotificationResponseDto>.Success(MapReadResponse(notification), "Notification is already read.");
                }

                notification.IsRead = true;

                _dbContext.Notifications.Update(notification);
                await _dbContext.SaveChangesAsync();

                return Result<ReadNotificationResponseDto>.Success(MapReadResponse(notification), "Notification marked as read successfully.");
                #endregion
            }
            catch (Exception ex)
            {
                return Result<ReadNotificationResponseDto>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }

        private static NotificationEntity MapToEntity(CreateNotificationRequestDto request)
        {
            return new NotificationEntity
            {
                UserId = request.UserId,
                Type = ResolveNotificationType(request.NotificationType),
                Message = request.Message,
                IsRead = false,
                IsDelete = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static CreateNotificationResponseDto MapCreateResponse(NotificationEntity notification)
        {
            return new CreateNotificationResponseDto
            {
                NotificationId = notification.Id,
                UserId = notification.UserId ?? 0,
                NotificationType = notification.Type.ToString(),
                Message = notification.Message
            };
        }

        private static ReadNotificationResponseDto MapReadResponse(NotificationEntity notification)
        {
            return new ReadNotificationResponseDto
            {
                NotificationId = notification.Id,
                IsRead = notification.IsRead ?? false
            };
        }

        private static DeleteNotificationResponseDto MapDeleteResponse(NotificationEntity notification)
        {
            return new DeleteNotificationResponseDto
            {
                NotificationId = notification.Id,
                IsDeleted = notification.IsDelete ?? false
            };
        }

        private static NotificationType ResolveNotificationType(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value) &&
                Enum.TryParse<NotificationType>(value.Replace("-", "_"), ignoreCase: true, out var parsedType))
            {
                return parsedType;
            }

            return NotificationType.System;
        }

        private string GetTitleForType(string? type = null)
        {
            return type?.ToLower() switch
            {
                "invitation" => "New Monastery Invitation",
                "donation" => "Donation Update",
                "system" => "System Alert",
                "hsum_chaint" => "Hsum Chaint Notification",
                _ => "New Notification"
            };
        }
    }
}
