using FirebaseAdmin.Messaging;

namespace HsumChaint.Domain.Features.Notification.Providers
{
    public class FirebaseNotificationProvider : IFirebaseNotificationProvider
    {
        public async Task<bool> SendPushAsync(string deviceToken, string? title, string? body, Dictionary<string, string> data)
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new FirebaseAdmin.Messaging.Notification()
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




