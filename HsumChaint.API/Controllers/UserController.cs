using HsumChaint.Application.DTOs.User;
using HsumChaint.Application.ServiceInterfaces;

using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Controllers
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
        public async Task<IActionResult> GetAllUsers()
        {
            var userList = await _userService.GetAllUsers();

            if (userList == null)
                return NotFound();

            return Ok(userList);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUser(id);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut]
        public async Task<IActionResult> PutUser(UserDto user)
        {
            var updatedResult = await _userService.PutUser(user);

            if (updatedResult == null)
                return NotFound();

            return Ok(updatedResult);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deletedResult = await _userService.DeleteUser(id);

            if (deletedResult == null)
                return NotFound();

            return Ok(deletedResult);
        }

        #region Invitation

        // GET Invitations List from user
        [HttpGet("{id}/invitations")]
        public async Task<IActionResult> GetUserInvitationList(int id)
        {
            var invitationList = await _userService.GetUserInvitationList(id);

            if (invitationList == null)
                return NotFound();

            return Ok(invitationList);
        }

        // GET List of Invited By Other User
        [HttpGet("{id}/invited-by-list")]
        public async Task<IActionResult> GetInvitedByOtherList(int id)
        {
            var invitedByOtherList = await _userService.GetInvitedByOtherList(id);

            if (invitedByOtherList == null)
                return NotFound();

            return Ok(invitedByOtherList);
        }

        #endregion

        #region Notification

        // GET Invitations List for user
        [HttpGet("{id}/notification")]
        public async Task<IActionResult> GetUserNotificationList(int id)
        {
            var notificationList = await _userService.GetUserNotificationList(id);

            if (notificationList == null)
                return NotFound();

            return Ok(notificationList);
        }

        // GET List of Invited By Other User
        [HttpDelete("{id}/notification")]
        public async Task<IActionResult> DeleteUserNotificationList(int id)
        {
            var deletedNotificationResult = await _userService.DeleteUserNotificationList(id);

            if (deletedNotificationResult == null)
                return NotFound();

            return Ok(deletedNotificationResult);
        }

        #endregion
    }
}
