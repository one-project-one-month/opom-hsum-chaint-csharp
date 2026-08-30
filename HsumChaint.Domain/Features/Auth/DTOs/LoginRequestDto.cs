using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Auth.DTOs
{
    public class LoginRequestDto
    {
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
    }
}




