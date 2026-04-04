using HsumChaint.Common.CommonEnum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public UserType UserType { get; set; }

    }
}
