using System.Threading.Tasks;
using HsumChaint.API.Extensions;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HsumChaint.API.Features.Auth.Controllers
{
    [Route("api/v1/[controller]/")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILoginModeService _loginModeService;
        private readonly IOtpService _otpService;

        public AuthController(
            IAuthService authService,
            ILoginModeService loginModeService,
            IOtpService otpService)
        {
            _authService = authService;
            _loginModeService = loginModeService;
            _otpService = otpService;
        }

        #region Register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto reqModel, [FromServices] AppDbContext db)
        {
            var publicRole = await db.Roles.AnyAsync(role => role.Id == reqModel.RoleId && !role.IsDeleted
                && (role.Name == "User" || role.Name == "Monk" || role.Name == "Donor"));
            if (!publicRole)
            {
                return Forbid();
            }

            var registerResponse = await _authService.Register(reqModel);
            return registerResponse.ToActionResult();
        }
        #endregion

        #region LoginMode
        [HttpGet("login-mode")]
        public async Task<IActionResult> LoginMode()
        {
            var mode = await _loginModeService.GetActiveMode();
            if (mode.IsFailure || mode.Data is null)
            {
                return mode.ToActionResult();
            }

            return Result<PublicLoginConfigurationDto>.Success(new PublicLoginConfigurationDto(
                mode.Data.Code,
                mode.Data.RequirePhoneVerification,
                mode.Data.RequireEmailVerification)).ToActionResult();
        }
        #endregion

        #region OTP
        [HttpPost("otp/request")]
        public async Task<IActionResult> RequestOtp(RequestOtpDto request)
        {
            var result = await _otpService.Request(request);
            return result.ToActionResult();
        }

        [HttpPost("otp/verify")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto request)
        {
            var result = await _otpService.Verify(request);
            return result.ToActionResult();
        }
        #endregion

        #region Login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto reqModel)
        {
            var loginResponse = await _authService.Login(reqModel);
            return loginResponse.ToActionResult();
        }
        #endregion

        #region RefreshToken
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto reqModel)
        {
            var refreshTokenResponse = await _authService.RefreshTokens(reqModel);
            return refreshTokenResponse.ToActionResult();
        }
        #endregion
    }
}
