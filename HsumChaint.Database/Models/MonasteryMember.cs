using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class MonasteryMember
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? MonasterySpaceId { get; set; }

    public int RoleId { get; set; }

    public bool? IsOwner { get; set; }

    public virtual Role? Role { get; set; }
}
