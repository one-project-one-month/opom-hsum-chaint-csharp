using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Auth.DTOs
{
    public class GenerateRefreshTokenDto
    {

        public int? UserId { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? ExpiresAt { get; set; }

    }
}




