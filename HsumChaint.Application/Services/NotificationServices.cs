using AutoMapper;
using HsumChaint.Application.DTOs;
using HsumChaint.Application.DTOs.Notification;
using HsumChaint.Application.ServiceInterfaces;
using HsumChaint.Infrastructure.Models;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.Services
{
    public class NotificationServices : INotificationService
    {
        private readonly IAuthRepository _authRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IFirebaseNotificationProvider _notificationProvider;
        private readonly IMapper _mapper;

        public NotificationServices(IAuthRepository authRepository, INotificationRepository notificationRepository, IFirebaseNotificationProvider notificationProvider,
            IMapper mapper)
        {
            _authRepository = authRepository;
            _notificationRepository = notificationRepository;
            _notificationProvider = notificationProvider;
            _mapper = mapper;
        }

        public async Task<ApplicationCommonResponseModel<DeleteNotificationResponseDto>> DeleteNotification(DeleteNotificationRequestDto requestModel)
        {
            var response = new ApplicationCommonResponseModel<DeleteNotificationResponseDto>();

            try
            {
                #region Fetch and Validate Notification
                var notificationResult = await _notificationRepository.GetById(requestModel.NotificationId);

                if (notificationResult.Data == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Notification not found.";
                    return response;
                }

                if (notificationResult.Data.UserId != requestModel.UserId)
                {
                    response.IsSuccess = false;
                    response.Message = "Unauthorized access to this notification.";
                    return response;
                }
                #endregion

                #region Update Database
                if (notificationResult.Data.IsDelete == true)
                {
                    response.IsSuccess = true;
                    response.Message = "Notification is already deleted.";
                    response.Data = _mapper.Map<DeleteNotificationResponseDto>(notificationResult.Data);
                    return response;
                }

                notificationResult.Data.IsDelete = true;

                var dbNotification = await _notificationRepository.Update(notificationResult.Data);

                response.IsSuccess = true;
                response.Message = "Notification deleted successfully.";
                response.Data = _mapper.Map<DeleteNotificationResponseDto>(dbNotification.Data);
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
                //var user = await _authRepository.GetUserById(requestModel.UserId);
                //if (user.Data == null)
                //{
                //    response.IsSuccess = false;
                //    response.Message = "User not found";

                //    return response;
                //}
                #endregion

                #region Get FCM token and Store Notification into DB
                var deviceFcmToken = "cXHoVuz38pPly3vAQxJjY9:APA91bHLmbx-5Zx8WsazisdRtHqUCgeVkrve5kEKrDf83xt4WzpS9h2dQ_uHqz5Z37loV1xI9vc5UEcXTHxDjAiYSu9G5HxSwqgefEztY0GZLFV1cnXH1nc";
                //var deviceFcmToken = user.Data.FcmToken;

                var notification = _mapper.Map<Notification>(requestModel);

                var dbNotification = await _notificationRepository.Create(notification);

                response.IsSuccess = true;
                response.Message = dbNotification.Message;

                response.Data = _mapper.Map<CreateNotificationResponseDto>(dbNotification.Data);
                #endregion

                #region Send Notification
                if (!string.IsNullOrEmpty(deviceFcmToken) && dbNotification.Data != null)
                {
                    var title = GetTitleForType(requestModel.NotificationType);

                    var payloadData = new Dictionary<string, string>
                    {
                        { "notificationId", dbNotification.Data.Id.ToString() },
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
                var notificationResult = await _notificationRepository.GetById(requestModel.NotificationId);

                if (notificationResult.Data == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Notification not found.";
                    return response;
                }

                if (notificationResult.Data.UserId != requestModel.UserId)
                {
                    response.IsSuccess = false;
                    response.Message = "Unauthorized access to this notification.";
                    return response;
                }
                #endregion

                #region Update Database
                if (notificationResult.Data.IsRead == true)
                {
                    response.IsSuccess = true;
                    response.Message = "Notification is already read.";
                    response.Data = _mapper.Map<ReadNotificationResponseDto>(notificationResult.Data);
                    return response;
                }

                notificationResult.Data.IsRead = true;

                var dbNotification = await _notificationRepository.Update(notificationResult.Data);

                response.IsSuccess = true;
                response.Message = "Notification marked as read successfully.";
                response.Data = _mapper.Map<ReadNotificationResponseDto>(dbNotification.Data);
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
