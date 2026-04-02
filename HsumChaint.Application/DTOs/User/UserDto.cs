using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.DTOs.User
{
    public class UserDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserType { get; set; }
        public string? MonasteryName { get; set; }

        public string? MonasteryAddress { get; set; }

        public string? Email { get; set; }

        public string? ContactPhoneNumber { get; set; }
    }
}
