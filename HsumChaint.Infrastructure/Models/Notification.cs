using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HsumChaint.Infrastructure.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Type { get; set; }

    public string? Message { get; set; }

    public bool? IsRead { get; set; }
    public bool? IsDelete { get; set; }

    [Column("CreatedAt")]
    public DateTime? CreatedAt { get; set; }
}
