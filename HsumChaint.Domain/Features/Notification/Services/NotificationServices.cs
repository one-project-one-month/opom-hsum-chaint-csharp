using AutoMapper;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Notification.DTOs;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Domain.Features.Notification.ServiceInterfaces;
using NotificationEntity = HsumChaint.Database.Models.Notification;

namespace HsumChaint.Domain.Features.Notification.Services
{
    public class NotificationServices : INotificationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IFirebaseNotificationProvider _notificationProvider;
        private readonly IMapper _mapper;

        public NotificationServices(AppDbContext dbContext, IFirebaseNotificationProvider notificationProvider, IMapper mapper)
        {
            _dbContext = dbContext;
            _notificationProvider = notificationProvider;
            _mapper = mapper;
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
                    response.Data = _mapper.Map<DeleteNotificationResponseDto>(notification);
                    return response;
                }

                notification.IsDelete = true;

                _dbContext.Notifications.Update(notification);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Notification deleted successfully.";
                response.Data = _mapper.Map<DeleteNotificationResponseDto>(notification);
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
                //var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == requestModel.UserId && x.IsDeleted == false);
                //if (user == null)
                //{
                //    response.IsSuccess = false;
                //    response.Message = "User not found";

                //    return response;
                //}
                #endregion

                #region Get FCM token and Store Notification into DB
                var deviceFcmToken = "cXHoVuz38pPly3vAQxJjY9:APA91bHLmbx-5Zx8WsazisdRtHqUCgeVkrve5kEKrDf83xt4WzpS9h2dQ_uHqz5Z37loV1xI9vc5UEcXTHxDjAiYSu9G5HxSwqgefEztY0GZLFV1cnXH1nc";
                //var deviceFcmToken = user.FcmToken;

                var notification = _mapper.Map<NotificationEntity>(requestModel);

                await _dbContext.Notifications.AddAsync(notification);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Notification added successfully.";

                response.Data = _mapper.Map<CreateNotificationResponseDto>(notification);
                #endregion

                #region Send Notification
                if (!string.IsNullOrEmpty(deviceFcmToken) && notification != null)
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
                    response.Data = _mapper.Map<ReadNotificationResponseDto>(notification);
                    return response;
                }

                notification.IsRead = true;

                _dbContext.Notifications.Update(notification);
                await _dbContext.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Notification marked as read successfully.";
                response.Data = _mapper.Map<ReadNotificationResponseDto>(notification);
                #endregion
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Application layer err: {ex.Message} {ex.InnerException}";
            }

            return response;
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
