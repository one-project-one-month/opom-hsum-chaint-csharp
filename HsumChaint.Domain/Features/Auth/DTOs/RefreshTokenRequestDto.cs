using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Auth.DTOs
{
    public class RefreshTokenRequestDto
    {
        public int UserId { get; set; }
        public string RefreshToken { get; set; }
    }
}




