using System;

namespace HsumChaint.Database.Models
{
    public partial class OtpCode
    {
        public long Id { get; set; }

        public int? UserId { get; set; }

        public string Channel { get; set; } = null!;

        public string Purpose { get; set; } = null!;

        public string Destination { get; set; } = null!;

        public string CodeHash { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public int FailedAttemptCount { get; set; }

        public int MaxAttemptCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual User? User { get; set; }
    }
}
