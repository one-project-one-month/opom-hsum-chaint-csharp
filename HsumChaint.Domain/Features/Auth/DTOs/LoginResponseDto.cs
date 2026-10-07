using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.Auth.DTOs
{
    public class LoginResponseDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
        public int ID { get; set; }
    }
}




