using FirebaseAdmin.Messaging;
using HsumChaint.Infrastructure.RepositoryInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Infrastructure.Providers
{
    public class FirebaseNotificationProvider : IFirebaseNotificationProvider
    {
        public async Task<bool> SendPushAsync(string deviceToken, string? title, string? body, Dictionary<string, string> data)
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body,
                },
                Data = data
            };

            try
            {
                var isSend = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
