using System.Threading.Tasks;
using HsumChaint.API.Extensions;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Shared;
using Microsoft.AspNetCore.Authorization;
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
        [HttpPost("register/request-otp")]
        public async Task<IActionResult> RequestRegisterOtp([FromBody] RequestOtpDto request)
        {
            request.Channel = OtpChannels.Email;
            request.Purpose = OtpPurposes.VerifyEmail;
            var result = await _otpService.Request(request);
            return result.ToActionResult();
        }

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

        #region ForgotPassword & ResetPassword
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] RequestOtpDto request)
        {
            request.Channel = OtpChannels.Email;
            request.Purpose = OtpPurposes.ResetPassword;
            var result = await _otpService.Request(request);
            return result.ToActionResult();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var result = await _authService.ResetPassword(request);
            return result.ToActionResult();
        }
        #endregion

        #region ChangePassword
        [Authorize]
        [HttpPost("change-password/request-otp")]
        public async Task<IActionResult> RequestChangePasswordOtp([FromServices] AppDbContext db)
        {
            var userId = User.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsDeleted == false);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest("User does not have an active registered email address.");
            }

            var result = await _otpService.Request(new RequestOtpDto
            {
                Destination = user.Email,
                Channel = OtpChannels.Email,
                Purpose = OtpPurposes.ChangePassword
            });
            return result.ToActionResult();
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var userId = User.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _authService.ChangePassword(userId.Value, request);
            return result.ToActionResult();
        }
        #endregion
    }
}
