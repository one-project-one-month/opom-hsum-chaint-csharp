using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Configuration;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.Events;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HsumChaint.Domain.Features.Auth.Services
{
    public class OtpService : IOtpService
    {
        private readonly AppDbContext _context;
        private readonly ILoginModeService _loginModes;
        private readonly IEmailSender _emailSender;
        private readonly IPasswordHasher<OtpCode> _hasher;
        private readonly OtpOptions _options;
        private readonly IMediator _mediator;

        public OtpService(
            AppDbContext context,
            ILoginModeService loginModes,
            IEmailSender emailSender,
            IPasswordHasher<OtpCode> hasher,
            IOptions<OtpOptions> options,
            IMediator mediator)
        {
            _context = context;
            _loginModes = loginModes;
            _emailSender = emailSender;
            _hasher = hasher;
            _options = options.Value;
            _mediator = mediator;
        }

        #region Request OTP
        public async Task<Result<OtpResponseDto>> Request(RequestOtpDto request)
        {
            try
            {
                var destination = request.Destination?.Trim().ToLowerInvariant();
                var channel = request.Channel?.Trim().ToUpperInvariant();
                var purpose = request.Purpose?.Trim().ToUpperInvariant();

                if (channel != OtpChannels.Email || string.IsNullOrWhiteSpace(destination) || !destination.Contains('@') || !ValidPurpose(purpose))
                {
                    return Result<OtpResponseDto>.Failure("Invalid OTP request.");
                }

                var modeResult = await _loginModes.GetActiveMode();
                if (modeResult.IsFailure || modeResult.Data is null)
                {
                    return Result<OtpResponseDto>.Failure(modeResult.Message);
                }

                var mode = modeResult.Data;
                if (mode.Code == LoginModeCodes.PhoneOnly && !mode.RequireEmailVerification)
                {
                    return Result<OtpResponseDto>.Failure("Email OTP is not supported by the active login mode.");
                }

                var now = DateTime.UtcNow;
                var recent = await _context.OtpCodes
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(x => x.Destination == destination && x.Channel == channel && x.Purpose == purpose);

                if (recent != null && recent.CreatedAt.AddSeconds(_options.ResendCooldownSeconds) > now)
                {
                    return Result<OtpResponseDto>.Failure("Please wait before requesting another OTP.");
                }

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == destination && x.IsDeleted == false);
                if (purpose is OtpPurposes.VerifyEmail or OtpPurposes.Login or OtpPurposes.ResetPassword or OtpPurposes.ChangePassword && user == null)
                {
                    return Result<OtpResponseDto>.Failure("Unable to send OTP.");
                }

                // Revoke previous unused codes for the destination
                await _context.OtpCodes
                    .Where(x => x.Destination == destination && x.Channel == channel && x.Purpose == purpose && !x.IsUsed && x.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now));

                var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                var expires = now.AddMinutes(_options.LifetimeMinutes);

                var otp = new OtpCode
                {
                    Id = NewId(),
                    UserId = user?.Id,
                    Channel = channel,
                    Purpose = purpose!,
                    Destination = destination,
                    ExpiresAt = expires,
                    MaxAttemptCount = _options.MaxAttempts,
                    CreatedAt = now
                };
                otp.CodeHash = _hasher.HashPassword(otp, code);

                await _context.OtpCodes.AddAsync(otp);
                await _context.SaveChangesAsync();

                try
                {
                    await _emailSender.SendOtpAsync(destination, code, expires, purpose);
                }
                catch
                {
                    otp.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Result<OtpResponseDto>.Failure("Unable to send OTP.");
                }

                await _mediator.Publish(new CleanupExpiredOtpEvent());

                return Result<OtpResponseDto>.Success(new OtpResponseDto(expires, _options.ResendCooldownSeconds), "OTP has been sent.");
            }
            catch (Exception ex)
            {
                return Result<OtpResponseDto>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion

        #region Verify OTP
        public async Task<Result> Verify(VerifyOtpDto request)
        {
            try
            {
                var destination = request.Destination?.Trim().ToLowerInvariant();
                var channel = request.Channel?.Trim().ToUpperInvariant();
                var purpose = request.Purpose?.Trim().ToUpperInvariant();
                var now = DateTime.UtcNow;

                var otp = await _context.OtpCodes
                    .Include(x => x.User)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(x => x.Destination == destination && x.Channel == channel && x.Purpose == purpose);

                if (otp == null || otp.IsUsed || otp.RevokedAt != null || otp.ExpiresAt <= now || otp.FailedAttemptCount >= otp.MaxAttemptCount)
                {
                    return Result.Failure("Invalid or expired OTP.");
                }

                if (_hasher.VerifyHashedPassword(otp, otp.CodeHash, request.Code ?? string.Empty) == PasswordVerificationResult.Failed)
                {
                    otp.FailedAttemptCount++;
                    if (otp.FailedAttemptCount >= otp.MaxAttemptCount)
                    {
                        otp.RevokedAt = now;
                    }
                    await _context.SaveChangesAsync();
                    return Result.Failure("Invalid or expired OTP.");
                }

                await using var tx = await _context.Database.BeginTransactionAsync();

                otp.IsUsed = true;
                otp.UsedAt = now;

                if (otp.User != null && purpose == OtpPurposes.VerifyEmail)
                {
                    otp.User.EmailVerifiedAt = now;
                }

                if (otp.User != null && purpose == OtpPurposes.VerifyPhone)
                {
                    otp.User.PhoneVerifiedAt = now;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                await _mediator.Publish(new CleanupExpiredOtpEvent());

                return Result.Success("OTP verified.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion

        #region Helpers
        private static bool ValidPurpose(string? value)
        {
            return value is OtpPurposes.VerifyEmail or OtpPurposes.VerifyPhone or OtpPurposes.Login or OtpPurposes.ResetPassword or OtpPurposes.ChangePassword;
        }

        private static long NewId()
        {
            return (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() << 20) | (uint)RandomNumberGenerator.GetInt32(0, 1 << 20);
        }

        #endregion
    }
}
