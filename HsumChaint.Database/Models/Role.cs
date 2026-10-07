using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<MonasteryMember> MonasteryMembers { get; set; } = new List<MonasteryMember>();

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
