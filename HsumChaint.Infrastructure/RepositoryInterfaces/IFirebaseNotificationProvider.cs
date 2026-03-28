using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Infrastructure.RepositoryInterfaces
{
    public interface IFirebaseNotificationProvider
    {
        Task<bool> SendPushAsync(string deviceToken, string? title, string? body, Dictionary<string, string> data);
    }
}
