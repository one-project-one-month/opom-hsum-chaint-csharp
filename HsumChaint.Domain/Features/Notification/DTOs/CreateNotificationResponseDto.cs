using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Notification.DTOs
{
    public class CreateNotificationResponseDto
    {
        public int UserId { get; set; }
        public string? NotificationType { get; set; }
        public string? Message { get; set; }
        public int NotificationId { get; set;  } 
    }
}




