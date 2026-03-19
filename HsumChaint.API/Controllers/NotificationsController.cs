using HsumChaint.Application.DTOs;
using HsumChaint.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequestDto requestModel)
        {
            if(requestModel == null || requestModel.UserId < 0)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            var response = await _notificationService.SendNotificationAndStore(requestModel);

            if (response)
            {
                return Ok(new { message = "Notification created and pushed successfully." });
            }

            return NotFound(new { message = "User not found." });
        }
    }
}
