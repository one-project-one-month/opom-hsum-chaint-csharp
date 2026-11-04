
using HsumChaint.Shared;
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
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? Email { get; set; }
        public string? ContactPhoneNumber { get; set; }
    }

    public class GetUserRequestDto
    {
        public int Id { get; set; }
    }

    public class DeleteUserRequestDto
    {
        public int Id { get; set; }
    }

    public class GetUserInvitationsRequestDto
    {
        public int Id { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetInvitedByOtherRequestDto
    {
        public int Id { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetUserNotificationsRequestDto
    {
        public int Id { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetUserNotificationsFeedRequestDto : CursorPaginationRequest
    {
        public int Id { get; set; }
    }

    public class DeleteUserNotificationsRequestDto
    {
        public int Id { get; set; }
    }
}
