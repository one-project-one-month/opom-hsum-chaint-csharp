using HsumChaint.Shared;
using HsumChaint.API.Extensions;
using HsumChaint.API.Authorization;
using HsumChaint.Shared.Authorization;
using HsumChaint.Domain.Features.User.DTOs;
using HsumChaint.Domain.Features.User.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Features.User.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetAllUsers([FromQuery] PaginationRequest pagination)
        {
            var userList = await _userService.GetAllUsers(pagination);

            return userList.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var user = await _userService.GetUser(id);

            return user.ToActionResult();
        }

        [HttpPut]
        [HasPermission(Permissions.Users.Update)]
        public async Task<IActionResult> PutUser(UserDto user)
        {
            var currentUserId = User.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin") || (User.HasClaim("permission", Permissions.Users.Manage) && User.HasClaim("permission", Permissions.Roles.Assign));
            if (!isAdmin)
            {
                if (currentUserId.Value != user.Id)
                {
                    return Forbid();
                }

                // Normal users cannot elevate or alter their role
                var existingUser = await _userService.GetUser(user.Id);
                if (existingUser.Data != null)
                {
                    user.RoleId = existingUser.Data.RoleId;
                }
            }

            var updatedResult = await _userService.PutUser(user);

            return updatedResult.ToActionResult();
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Users.Manage)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deletedResult = await _userService.DeleteUser(id);

            return deletedResult.ToActionResult();
        }

        #region Invitation

        // GET Invitations List received by user
        [HttpGet("{id}/invitations")]
        [HasPermission(Permissions.Invitation.View)]
        public async Task<IActionResult> GetUserInvitationList(int id, [FromQuery] PaginationRequest pagination)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var invitationList = await _userService.GetUserInvitationList(id, pagination);

            return invitationList.ToActionResult();
        }

        // GET Invitations Feed received by user (Mobile Cursor Pagination)
        [HttpGet("{id}/invitations/feed")]
        [HasPermission(Permissions.Invitation.View)]
        public async Task<IActionResult> GetUserInvitationFeed(int id, [FromQuery] CursorPaginationRequest pagination)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var invitationFeed = await _userService.GetUserInvitationFeed(id, pagination);

            return invitationFeed.ToActionResult();
        }

        // GET List of Invitations sent by user
        [HttpGet("{id}/invited-by-list")]
        [HasPermission(Permissions.Invitation.View)]
        public async Task<IActionResult> GetInvitedByOtherList(int id, [FromQuery] PaginationRequest pagination)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var invitedByOtherList = await _userService.GetInvitedByOtherList(id, pagination);

            return invitedByOtherList.ToActionResult();
        }

        #endregion

        #region Notification

        // GET Notification List for user
        [HttpGet("{id}/notification")]
        [HasPermission(Permissions.Notification.View)]
        public async Task<IActionResult> GetUserNotificationList(int id, [FromQuery] PaginationRequest pagination)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var notificationList = await _userService.GetUserNotificationList(id, pagination);

            return notificationList.ToActionResult();
        }

        // GET Notification Feed for user (Mobile Cursor)
        [HttpGet("{id}/notification/feed")]
        [HasPermission(Permissions.Notification.View)]
        public async Task<IActionResult> GetUserNotificationFeed(int id, [FromQuery] CursorPaginationRequest pagination)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var notificationFeed = await _userService.GetUserNotificationFeed(id, pagination);

            return notificationFeed.ToActionResult();
        }

        // DELETE Notification List for user
        [HttpDelete("{id}/notification")]
        [HasPermission(Permissions.Notification.Manage)]
        public async Task<IActionResult> DeleteUserNotificationList(int id)
        {
            if (!CanAccessUserResource(id, Permissions.Users.Manage))
            {
                return Forbid();
            }

            var deletedNotificationResult = await _userService.DeleteUserNotificationList(id);

            return deletedNotificationResult.ToActionResult();
        }

        #endregion

        #region MonkProfile

        [HttpGet("{id}/monk-profile")]
        public async Task<IActionResult> GetMonkProfile(int id)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var currentUserId = User.GetCurrentUserId();
            var response = await _userService.GetMonkProfile(currentUserId ?? 0, id);
            return response.ToActionResult();
        }

        [HttpPatch("{id}/monk-profile")]
        public async Task<IActionResult> UpdateMonkProfile(int id, UpdateMonkProfileRequestDto? request)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            if (request == null)
            {
                return Result<MonkProfileDto>.Failure("Request data is required.").ToActionResult();
            }

            var currentUserId = User.GetCurrentUserId();
            var response = await _userService.UpdateMonkProfile(currentUserId ?? 0, id, request);
            return response.ToActionResult();
        }

        [HttpDelete("{id}/monk-profile")]
        public async Task<IActionResult> DeleteMonkProfile(int id)
        {
            if (!CanAccessUserResource(id, Permissions.Users.Manage))
            {
                return Forbid();
            }

            var currentUserId = User.GetCurrentUserId();
            var response = await _userService.DeleteMonkProfile(currentUserId ?? 0, id);
            return response.ToActionResult();
        }

        #endregion

        #region UserSettings

        [HttpGet("{id}/settings")]
        public async Task<IActionResult> GetUserSettings(int id)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var currentUserId = User.GetCurrentUserId();
            var response = await _userService.GetUserSettings(currentUserId ?? 0, id);
            return response.ToActionResult();
        }

        [HttpPut("{id}/settings")]
        public async Task<IActionResult> UpdateUserSettings(int id, UpdateUserSettingRequestDto request)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            var currentUserId = User.GetCurrentUserId();
            var response = await _userService.UpdateUserSettings(currentUserId ?? 0, id, request);
            return response.ToActionResult();
        }

        #endregion

        private bool CanAccessUserResource(int targetUserId, string adminPermission = Permissions.Users.View)
        {
            var currentUserId = User.GetCurrentUserId();
            return currentUserId.HasValue && (currentUserId.Value == targetUserId || User.IsInRole("Admin") || User.HasClaim("permission", adminPermission));
        }
    }
}
