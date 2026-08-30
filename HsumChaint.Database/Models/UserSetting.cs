using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class UserSetting
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public DateTime? PickupTime { get; set; }

    public DateTime? DropoffTime { get; set; }

    public DateTime? DropoffNotificationTime { get; set; }

    public DateTime? PickupNotificationTime { get; set; }
}
