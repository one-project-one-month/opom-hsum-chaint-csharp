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

        [HttpPost]
        [Route("AddUser")]
        public async Task<IActionResult> GetAddUser(UserDto reqModel)
        {
            var user = await _userService.AddUser(reqModel);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(UserDto user)
        {
            var updatedResult = await _userService.PutUser(user);

            if (updatedResult == null)
                return NotFound();

            return Ok(user);
        }
    }
}
