using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.DTOs.Notification
{
    public class ReadNotificationResponseDto
    {
        public int NotificationId { get; set; }
        public bool IsRead { get; set; }
    }
}
