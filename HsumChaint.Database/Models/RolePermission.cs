using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class RolePermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual Permission Permission { get; set; } = null!;
}
