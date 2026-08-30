using HsumChaint.Shared.CommonEnum;
using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class MonasteryMember
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? MonasterySpaceId { get; set; }

    public MonasteryRole Role { get; set; }

    public bool? IsOwner { get; set; }
}
