using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Features.Auth.Controllers
{
    [Route("api/v1/[controller]/")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        #region Register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto reqModel)
        {
            var registerResponse = await _authService.Register(reqModel);
            
            if(registerResponse.IsSuccess == true)
            {
                return Ok(reqModel);
            }

            return BadRequest(registerResponse);
        }
        #endregion

        #region Login

        [HttpPost("login")]

        public async Task<IActionResult> Login(LoginRequestDto reqModel)
        {
            var loginResponse = await _authService.Login(reqModel);

            if(loginResponse.IsSuccess == true)
            {
                return Ok(loginResponse);
            }

            return BadRequest(loginResponse);
        }
        #endregion

        #region RefreshToken
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto reqModel)
        {
            var refreshTokenResponse = await _authService.RefreshTokens(reqModel);

            if(refreshTokenResponse.IsSuccess == true)
            {
                return Ok(refreshTokenResponse);
            }

            return Unauthorized(refreshTokenResponse);
        }
        #endregion
    }
}

