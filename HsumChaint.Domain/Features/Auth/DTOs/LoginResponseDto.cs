using HsumChaint.Shared.CommonEnum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Auth.DTOs
{
    public class LoginResponseDto
    {
        public UserType UserType { get; set; }
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
        public int ID { get; set; }
    }
}




