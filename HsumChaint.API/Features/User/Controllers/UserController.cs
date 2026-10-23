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
    [Route("api/[controller]")]
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

            var request = new GetUserRequestDto { Id = id };
            var user = await _userService.GetUser(request);

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
                var existingUser = await _userService.GetUser(new GetUserRequestDto { Id = user.Id });
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
            var request = new DeleteUserRequestDto { Id = id };
            var deletedResult = await _userService.DeleteUser(request);

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

            var request = new GetUserInvitationsRequestDto
            {
                Id = id,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
            var invitationList = await _userService.GetUserInvitationList(request);

            return invitationList.ToActionResult();
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

            var request = new GetInvitedByOtherRequestDto
            {
                Id = id,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
            var invitedByOtherList = await _userService.GetInvitedByOtherList(request);

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

            var request = new GetUserNotificationsRequestDto
            {
                Id = id,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
            var notificationList = await _userService.GetUserNotificationList(request);

            return notificationList.ToActionResult();
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

            var request = new DeleteUserNotificationsRequestDto { Id = id };
            var deletedNotificationResult = await _userService.DeleteUserNotificationList(request);

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
            var request = new GetMonkProfileRequestDto { UserId = id };
            var response = await _userService.GetMonkProfile(currentUserId ?? 0, request);
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

            request.UserId = id;
            var currentUserId = User.GetCurrentUserId();
            var response = await _userService.UpdateMonkProfile(currentUserId ?? 0, request);
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
            var request = new DeleteMonkProfileRequestDto { UserId = id };
            var response = await _userService.DeleteMonkProfile(currentUserId ?? 0, request);
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
            var request = new GetUserSettingsRequestDto { UserId = id };
            var response = await _userService.GetUserSettings(currentUserId ?? 0, request);
            return response.ToActionResult();
        }

        [HttpPut("{id}/settings")]
        public async Task<IActionResult> UpdateUserSettings(int id, UpdateUserSettingRequestDto request)
        {
            if (!CanAccessUserResource(id))
            {
                return Forbid();
            }

            request.UserId = id;
            var currentUserId = User.GetCurrentUserId();
            var response = await _userService.UpdateUserSettings(currentUserId ?? 0, request);
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
