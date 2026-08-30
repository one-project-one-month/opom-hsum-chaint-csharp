using HsumChaint.Shared.CommonEnum;
using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class Invitation
{
    public int Id { get; set; }

    public int? MonasterySpaceId { get; set; }

    public int? InvitedUserId { get; set; }

    public int? InvitedById { get; set; }

    public MonasteryRole Role { get; set; }

    public InvitationStatus Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}
