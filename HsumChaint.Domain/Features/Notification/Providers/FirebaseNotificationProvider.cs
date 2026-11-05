using FirebaseAdmin.Messaging;

namespace HsumChaint.Domain.Features.Notification.Providers
{
    public class FirebaseNotificationProvider : IFirebaseNotificationProvider
    {
        public async Task<bool> SendPushAsync(string deviceToken, string? title, string? body, Dictionary<string, string> data)
        {
            var message = new Message()
            {
#pragma warning disable CS0618 // Existing clients persist legacy FCM registration tokens; migrate them to FIDs before changing this property.
                Token = deviceToken,
#pragma warning restore CS0618
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = title,
                    Body = body,
                },
                Data = data
            };

            try
            {
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}




