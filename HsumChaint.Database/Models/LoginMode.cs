using System;

namespace HsumChaint.Database.Models
{
    public partial class LoginMode
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public string DisplayName { get; set; } = null!;

        public bool IsActive { get; set; }

        public bool RequirePhoneVerification { get; set; }

        public bool RequireEmailVerification { get; set; }

        public int? UpdatedById { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual User? UpdatedBy { get; set; }
    }
}
