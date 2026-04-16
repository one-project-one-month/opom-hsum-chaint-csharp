using HsumChaint.Common.CommonEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HsumChaint.Application.DTOs.User
{
    public class NotificationDto
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public NotificationType Type { get; set; }

        public string? Message { get; set; }

        public bool? IsRead { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
