using HsumChaint.Shared.CommonEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HsumChaint.Database.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public NotificationType Type { get; set; }

    public string? Message { get; set; }

    public bool? IsRead { get; set; }

    public bool? IsDeleted { get; set; }

    [NotMapped]
    public bool? IsDelete
    {
        get => IsDeleted;
        set => IsDeleted = value;
    }

    [Column("CreatedAt")]
    public DateTime? CreatedAt { get; set; }
}
