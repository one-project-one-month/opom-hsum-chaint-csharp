using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.DTOs.Notification
{
    public class CreateNotificationResponseDto
    {
        public int UserId { get; set; }
        public string? NotificationType { get; set; }
        public string? Message { get; set; }
        public int NotificationId { get; set;  } 
    }
}
