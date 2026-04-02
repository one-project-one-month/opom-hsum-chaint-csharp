using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.DTOs.Notification
{
    public class DeleteNotificationRequestDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
    }
}
