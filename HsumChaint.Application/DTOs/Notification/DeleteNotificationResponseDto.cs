using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.DTOs.Notification
{
    public class DeleteNotificationResponseDto
    {
        public int NotificationId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
