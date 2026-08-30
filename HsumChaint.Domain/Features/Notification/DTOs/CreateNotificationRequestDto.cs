using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Notification.DTOs
{
    public class CreateNotificationRequestDto
    {
        public int UserId { get; set;  }
        public string? NotificationType {  get; set; }
        public string? Message { get; set; }
    }
}




