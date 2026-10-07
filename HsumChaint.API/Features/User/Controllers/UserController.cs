using HsumChaint.Shared;
using HsumChaint.API.Extensions;
using HsumChaint.API.Authorization;
using HsumChaint.Shared.Authorization;
using HsumChaint.Domain.Features.User.DTOs;
using HsumChaint.Domain.Features.User.ServiceInterfaces;

using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Features.User.Controllers
{
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
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUser(id);

            return user.ToActionResult();
        }

        [HttpPut]
        [HasPermission(Permissions.Users.Manage)]
        [HasPermission(Permissions.Roles.Assign)]
        public async Task<IActionResult> PutUser(UserDto user)
        {
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

        // GET Invitations List from user
        [HttpGet("{id}/invitations")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUserInvitationList(int id, [FromQuery] PaginationRequest pagination)
        {
            var invitationList = await _userService.GetUserInvitationList(id, pagination);

            return invitationList.ToActionResult();
        }

        // GET List of Invited By Other User
        [HttpGet("{id}/invited-by-list")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetInvitedByOtherList(int id, [FromQuery] PaginationRequest pagination)
        {
            var invitedByOtherList = await _userService.GetInvitedByOtherList(id, pagination);

            return invitedByOtherList.ToActionResult();
        }

        #endregion

        #region Notification

        // GET Invitations List for user
        [HttpGet("{id}/notification")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUserNotificationList(int id, [FromQuery] PaginationRequest pagination)
        {
            var notificationList = await _userService.GetUserNotificationList(id, pagination);

            return notificationList.ToActionResult();
        }

        // GET List of Invited By Other User
        [HttpDelete("{id}/notification")]
        [HasPermission(Permissions.Users.Manage)]
        public async Task<IActionResult> DeleteUserNotificationList(int id)
        {
            var deletedNotificationResult = await _userService.DeleteUserNotificationList(id);

            return deletedNotificationResult.ToActionResult();
        }

        #endregion
    }
}

