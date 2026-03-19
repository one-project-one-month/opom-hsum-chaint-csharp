using HsumChaint.Application.DTOs;
using HsumChaint.Application.ServiceInterfaces;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.Services
{
    public class NotificationServices : INotificationService
    {
        private readonly IFirebaseNotificationProvider _notificationProvider;

        public NotificationServices(IFirebaseNotificationProvider notificationProvider)
        {
            _notificationProvider = notificationProvider;
        }

        public async Task<bool> SendNotificationAndStore(CreateNotificationRequestDto requestModel)
        {
            var deviceFcm = "cXHoVuz38pPly3vAQxJjY9:APA91bHLmbx-5Zx8WsazisdRtHqUCgeVkrve5kEKrDf83xt4WzpS9h2dQ_uHqz5Z37loV1xI9vc5UEcXTHxDjAiYSu9G5HxSwqgefEztY0GZLFV1cnXH1nc";

            if (!string.IsNullOrEmpty(deviceFcm))
            {
                var title = GetTitleForType("invitation");
                var payloadData = new Dictionary<string, string>
                {
                    { "notificationId", "1" },
                    {"type", requestModel.NotificationType ?? "" }
                };

                await _notificationProvider.SendPushAsync(deviceFcm, title, requestModel.Message, payloadData);
            }

            return true;
        }

        private string GetTitleForType(string type)
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
