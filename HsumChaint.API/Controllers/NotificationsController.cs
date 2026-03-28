using HsumChaint.Application.DTOs.Notification;
using HsumChaint.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    //[Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [Route("create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequestDto requestModel)
        {
            if(requestModel == null || requestModel.UserId < 0)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            
            var notiResponse = await _notificationService.SendNotificationAndStore(requestModel);

            if (notiResponse.IsSuccess == true)
            {
                return Ok(notiResponse);
            }

            return BadRequest(notiResponse);
        }

        [Route("read-noti")]
        [HttpPut]
        public async Task<IActionResult> ReadNoti([FromBody] ReadNotificationRequestDto requestModel)
        {
            if (requestModel == null || requestModel.NotificationId <= 0 || requestModel.UserId <= 0)
            {
                return BadRequest(new { message = "Invalid request data. NotificationId and UserId are required." });
            }

            var response = await _notificationService.ReadNotification(requestModel);

            if (response.IsSuccess == true)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        [Route("delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteNotificationRequestDto requestModel)
        {
            if (requestModel == null || requestModel.NotificationId <= 0 || requestModel.UserId <= 0)
            {
                return BadRequest(new { message = "Invalid request data. NotificationId and UserId are required." });
            }

            var response = await _notificationService.DeleteNotification(requestModel);

            if (response.IsSuccess == true)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        
    }
}
