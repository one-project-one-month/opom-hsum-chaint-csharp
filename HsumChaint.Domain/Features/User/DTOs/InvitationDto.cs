using HsumChaint.Shared.CommonEnum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.User.DTOs
{
    public class InvitationDto
    {
        public int Id { get; set; }

        public int? MonasterySpaceId { get; set; }

        public int? InvitedUserId { get; set; }

        public int? InvitedById { get; set; }

        public MonasteryRole Role { get; set; }

        public InvitationStatus Status { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}




