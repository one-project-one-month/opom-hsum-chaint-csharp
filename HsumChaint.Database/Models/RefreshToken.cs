using System;
using System.Collections.Generic;

namespace HsumChaint.Database.Models;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? RefreshToken1 { get; set; }

    /// <summary>
    /// Friendly alias for the hashed refresh token (mapped from column 'refresh_token')
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? TokenHash
    {
        get => RefreshToken1;
        set => RefreshToken1 = value;
    }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
