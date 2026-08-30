using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? RefreshToken1 { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
