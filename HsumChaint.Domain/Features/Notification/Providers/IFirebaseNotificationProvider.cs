namespace HsumChaint.Domain.Features.Notification.Providers
{
    public interface IFirebaseNotificationProvider
    {
        Task<bool> SendPushAsync(string deviceToken, string? title, string? body, Dictionary<string, string> data);
    }
}






