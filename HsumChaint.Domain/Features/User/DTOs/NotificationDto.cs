using HsumChaint.Shared.CommonEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HsumChaint.Domain.Features.User.DTOs
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




