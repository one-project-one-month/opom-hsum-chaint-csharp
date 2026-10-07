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

        public async Task<ApplicationCommonResponseModel<DeleteNotificationResponseDto>> DeleteNotification(DeleteNotificationRequestDto requestModel)
        {
            var response = new ApplicationCommonResponseModel<DeleteNotificationResponseDto>();

            try
            {
                #region Fetch and Validate Notification
                var notification = await _dbContext.Notifications.FindAsync(requestModel.NotificationId);

                if (notification == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Notification not found.";
                    return response;
                }

                if (notification.UserId != requestModel.UserId)
                {
                    response.IsSuccess = false;
                    response.Message = "Unauthorized access to this notification.";
                    return response;
                }
                #endregion

                #region Update Database
                if (notification.IsDelete == true)
                {
                    response.IsSuccess = true;
                    response.Message = "Notification is already deleted.";
                    response.Data = MapDeleteResponse(notification);
                    return response;
                }

                notification.IsDelete = true;

                _dbContext.Notifications.Update(notification);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Notification deleted successfully.";
                response.Data = MapDeleteResponse(notification);
                #endregion
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application layer err: {ex.Message} {ex.InnerException}";
            }

            return response;
        }

        public async Task<ApplicationCommonResponseModel<CreateNotificationResponseDto>> SendNotificationAndStore(CreateNotificationRequestDto requestModel)
        {
            var response = new ApplicationCommonResponseModel<CreateNotificationResponseDto>();

            try
            {
                #region Check If User Exists
                var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == requestModel.UserId && x.IsDeleted == false);
                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found";

                    return response;
                }
                #endregion

                #region Get FCM token and Store Notification into DB
                var deviceFcmToken = user.FcmToken;

                var notification = MapToEntity(requestModel);

                await _dbContext.Notifications.AddAsync(notification);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Notification added successfully.";

                response.Data = MapCreateResponse(notification);
                #endregion

                #region Send Notification
                if (!string.IsNullOrWhiteSpace(deviceFcmToken) && notification != null)
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
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application layer err: {ex.Message} {ex.InnerException}";
            }

            return response;
        }

        public async Task<ApplicationCommonResponseModel<ReadNotificationResponseDto>> ReadNotification(ReadNotificationRequestDto requestModel)
        {
            var response = new ApplicationCommonResponseModel<ReadNotificationResponseDto>();

            try
            {
                #region Fetch and Validate Notification
                var notification = await _dbContext.Notifications.FindAsync(requestModel.NotificationId);

                if (notification == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Notification not found.";
                    return response;
                }

                if (notification.UserId != requestModel.UserId)
                {
                    response.IsSuccess = false;
                    response.Message = "Unauthorized access to this notification.";
                    return response;
                }
                #endregion

                #region Update Database
                if (notification.IsRead == true)
                {
                    response.IsSuccess = true;
                    response.Message = "Notification is already read.";
                    response.Data = MapReadResponse(notification);
                    return response;
                }

                notification.IsRead = true;

                _dbContext.Notifications.Update(notification);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Notification marked as read successfully.";
                response.Data = MapReadResponse(notification);
                #endregion
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application layer err: {ex.Message} {ex.InnerException}";
            }

            return response;
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
