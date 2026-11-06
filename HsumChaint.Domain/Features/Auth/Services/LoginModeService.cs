using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Shared;
using Microsoft.EntityFrameworkCore;

namespace HsumChaint.Domain.Features.Auth.Services
{
    public class LoginModeService : ILoginModeService
    {
        private readonly AppDbContext _context;

        public LoginModeService(AppDbContext context)
        {
            _context = context;
        }

        #region GetActiveMode
        public async Task<Result<LoginMode>> GetActiveMode()
        {
            try
            {
                var mode = await _context.LoginModes.FirstOrDefaultAsync(x => x.IsActive);
                if (mode != null)
                {
                    return Result<LoginMode>.Success(mode);
                }

                return Result<LoginMode>.Success(new LoginMode
                {
                    Id = 1,
                    Code = LoginModeCodes.PhoneOnly,
                    DisplayName = "Phone only",
                    IsActive = true
                });
            }
            catch (Exception ex)
            {
                return Result<LoginMode>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion

        #region GetAllModes
        public async Task<Result<IReadOnlyList<LoginModeDto>>> GetAllModes()
        {
            try
            {
                var modes = await _context.LoginModes
                    .OrderBy(x => x.Id)
                    .Select(x => new LoginModeDto
                    {
                        Code = x.Code,
                        DisplayName = x.DisplayName,
                        IsActive = x.IsActive,
                        RequirePhoneVerification = x.RequirePhoneVerification,
                        RequireEmailVerification = x.RequireEmailVerification
                    })
                    .ToListAsync();

                IReadOnlyList<LoginModeDto> result = modes.Count == 0
                    ? new List<LoginModeDto>
                    {
                        new LoginModeDto(LoginModeCodes.PhoneOnly, "Phone only", true, false, false),
                        new LoginModeDto(LoginModeCodes.EmailOnly, "Email only", false, false, false),
                        new LoginModeDto(LoginModeCodes.PhoneOrEmail, "Phone or email", false, false, false)
                    }
                    : modes;

                return Result<IReadOnlyList<LoginModeDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<LoginModeDto>>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion

        #region UpdateActiveMode
        public async Task<Result> UpdateActiveMode(int adminUserId, UpdateLoginModeRequestDto request)
        {
            try
            {
                var code = request.Code?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Result.Failure("Login mode code is required.");
                }

                if (request.RequirePhoneVerification)
                {
                    return Result.Failure("Phone verification is not available yet.");
                }

                var modes = await _context.LoginModes.ToListAsync();
                var selected = modes.FirstOrDefault(x => x.Code == code);
                if (selected == null)
                {
                    return Result.Failure("Invalid login mode code.");
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                foreach (var mode in modes)
                {
                    mode.IsActive = false;
                }

                selected.IsActive = true;
                selected.RequirePhoneVerification = false;
                selected.RequireEmailVerification = request.RequireEmailVerification;
                selected.UpdatedById = adminUserId;
                selected.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result.Success("Login mode updated.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion
    }
}
