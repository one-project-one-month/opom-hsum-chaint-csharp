using HsumChaint.Shared.CommonEnum;

using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain.Features.User.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public UserType UserType { get; set; }
        public string? Email { get; set; }
        public string? ContactPhoneNumber { get; set; }
    }
}




