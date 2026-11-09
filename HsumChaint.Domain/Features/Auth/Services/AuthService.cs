using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;
using HsumChaint.Shared;
using HsumChaint.Shared.Authorization;
using HsumChaint.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UserEntity = HsumChaint.Database.Models.User;

namespace HsumChaint.Domain.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHasher<UserEntity> _passwordHasher;
        private readonly JwtOptions _jwtOptions;
        private readonly IRolePermissionService _rolePermissionService;
        private readonly ILoginModeService _loginModeService;
        private readonly IPasswordHasher<OtpCode> _otpHasher;

        public AuthService(
            AppDbContext dbContext,
            IPasswordHasher<UserEntity> passwordHasher,
            IOptions<JwtOptions> jwtOptions,
            IRolePermissionService rolePermissionService,
            ILoginModeService loginModeService,
            IPasswordHasher<OtpCode> otpHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _jwtOptions = jwtOptions.Value;
            _rolePermissionService = rolePermissionService;
            _loginModeService = loginModeService;
            _otpHasher = otpHasher;
        }

        public AuthService(
            AppDbContext dbContext,
            IPasswordHasher<UserEntity> passwordHasher,
            IOptions<JwtOptions> jwtOptions,
            IRolePermissionService rolePermissionService,
            ILoginModeService loginModeService)
            : this(dbContext, passwordHasher, jwtOptions, rolePermissionService, loginModeService, new PasswordHasher<OtpCode>())
        {
        }

        public AuthService(
            AppDbContext dbContext,
            IPasswordHasher<UserEntity> passwordHasher,
            IOptions<JwtOptions> jwtOptions,
            IRolePermissionService rolePermissionService)
            : this(dbContext, passwordHasher, jwtOptions, rolePermissionService, new DefaultFallbackLoginModeService(), new PasswordHasher<OtpCode>())
        {
        }

        private sealed class DefaultFallbackLoginModeService : ILoginModeService
        {
            public Task<Result<LoginMode>> GetActiveMode() => Task.FromResult(Result<LoginMode>.Success(new LoginMode { Code = LoginModeCodes.PhoneOnly, IsActive = true }));
            public Task<Result<IReadOnlyList<LoginModeDto>>> GetAllModes() => throw new NotSupportedException();
            public Task<Result> UpdateActiveMode(int adminUserId, UpdateLoginModeRequestDto request) => throw new NotSupportedException();
        }

        #region Register
        public async Task<Result<LoginResponseDto>> Register(RegisterRequestDto reqModel)
        {
            try
            {
                if (!reqModel.Name.TryNormalizeRequiredString(out var normalizedName) || !reqModel.Password.TryNormalizeRequiredString(out _))
                {
                    return Result<LoginResponseDto>.Failure("Name and password are required.");
                }

                var modeResult = await _loginModeService.GetActiveMode();
                if (modeResult.IsFailure || modeResult.Data is null)
                {
                    return Result<LoginResponseDto>.Failure(modeResult.Message);
                }

                var mode = modeResult.Data;
                reqModel.Name = normalizedName;
                reqModel.PhoneNumber = reqModel.PhoneNumber.NormalizeOptionalString();
                reqModel.Email = reqModel.Email.NormalizeOptionalString()?.ToLowerInvariant();
                reqModel.ContactPhoneNumber = reqModel.ContactPhoneNumber.NormalizeOptionalString();
                var otpCode = reqModel.Otp.NormalizeOptionalString();

                if ((mode.Code == LoginModeCodes.PhoneOnly && reqModel.PhoneNumber is null)
                    || (mode.Code == LoginModeCodes.EmailOnly && reqModel.Email is null)
                    || (mode.Code == LoginModeCodes.PhoneOrEmail && reqModel.PhoneNumber is null && reqModel.Email is null))
                {
                    return Result<LoginResponseDto>.Failure("The active login mode requires a valid phone number or email address.");
                }

                if (!reqModel.Email.IsValidEmail())
                {
                    return Result<LoginResponseDto>.Failure("Email address is invalid.");
                }

                if (reqModel.PhoneNumber is not null && !Regex.IsMatch(reqModel.PhoneNumber, @"^\+?[0-9]+$"))
                {
                    return Result<LoginResponseDto>.Failure("Phone number must contain English digits only.");
                }

                if (reqModel.ContactPhoneNumber is string contactPhoneNumber
                    && !Regex.IsMatch(contactPhoneNumber, @"^\+?[0-9]+$"))
                {
                    return Result<LoginResponseDto>.Failure("Contact phone number must contain English digits only.");
                }

                if (!Regex.IsMatch(reqModel.Password!, @"^[\x21-\x7E]+$"))
                {
                    return Result<LoginResponseDto>.Failure("Password must contain English characters and symbols only.");
                }

                var selectedRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == reqModel.RoleId && !r.IsDeleted);
                if (selectedRole == null)
                {
                    return Result<LoginResponseDto>.Failure("Role not found.");
                }

                var existingUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.IsDeleted == false
                    && ((reqModel.PhoneNumber != null && x.PhoneNumber == reqModel.PhoneNumber)
                        || (reqModel.Email != null && x.Email == reqModel.Email)));

                var now = DateTime.UtcNow;
                var isAdminCreation = reqModel.GetType().Name == "AdminCreateUser";
                var requiresEmailVerification = !isAdminCreation && mode.RequireEmailVerification && !string.IsNullOrWhiteSpace(reqModel.Email);

                #region Duplicate / Unverified validation
                UserEntity userToLogin;
                if (existingUser != null)
                {
                    // If user registered earlier with this email but abandoned registration before verification, allow update and re-verification.
                    if (reqModel.Email != null && existingUser.Email == reqModel.Email && existingUser.EmailVerifiedAt == null)
                    {
                        existingUser.Name = reqModel.Name!;
                        existingUser.PhoneNumber = reqModel.PhoneNumber;
                        existingUser.Password = _passwordHasher.HashPasswordSafe(existingUser, reqModel.Password!);
                        existingUser.RoleId = reqModel.RoleId;
                        existingUser.ContactPhoneNumber = reqModel.ContactPhoneNumber;
                        existingUser.UpdatedAt = now;

                        if (selectedRole.Name == SystemRoles.Monk)
                        {
                            var monkProfile = await _dbContext.MonkProfiles.FirstOrDefaultAsync(m => m.UserId == existingUser.Id);
                            if (monkProfile == null)
                            {
                                await _dbContext.MonkProfiles.AddAsync(new MonkProfile
                                {
                                    UserId = existingUser.Id,
                                    MonasteryName = reqModel.MonasteryName,
                                    MonasteryAddress = reqModel.MonasteryAddress,
                                });
                            }
                            else
                            {
                                monkProfile.MonasteryName = reqModel.MonasteryName;
                                monkProfile.MonasteryAddress = reqModel.MonasteryAddress;
                            }
                        }

                        // If OTP was provided, verify and mark verified with auto-login; otherwise prompt to verify email with OTP.
                        if (!string.IsNullOrWhiteSpace(otpCode))
                        {
                            var otp = await _dbContext.OtpCodes
                                .OrderByDescending(x => x.CreatedAt)
                                .FirstOrDefaultAsync(x => x.Destination == reqModel.Email && x.Channel == OtpChannels.Email && x.Purpose == OtpPurposes.VerifyEmail);

                            if (otp != null && !otp.IsUsed && otp.RevokedAt == null && otp.ExpiresAt > now && otp.FailedAttemptCount < otp.MaxAttemptCount
                                && _otpHasher.VerifyHashedPassword(otp, otp.CodeHash, otpCode) != PasswordVerificationResult.Failed)
                            {
                                otp.IsUsed = true;
                                otp.UsedAt = now;
                                otp.UserId = existingUser.Id;
                                existingUser.EmailVerifiedAt = now;
                                await _dbContext.SaveChangesAsync();

                                var perms = await _rolePermissionService.GetUserPermissions(existingUser.Id);
                                string tok = _jwtOptions.GenerateJwtToken(existingUser.Id, existingUser.PhoneNumber ?? existingUser.Email!, selectedRole.Name, perms);
                                string refTok = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = existingUser.Id });

                                return Result<LoginResponseDto>.Success(new LoginResponseDto
                                {
                                    AccessToken = tok,
                                    RoleId = existingUser.RoleId,
                                    RoleName = selectedRole.Name,
                                    Permissions = perms,
                                    ID = existingUser.Id,
                                    RefreshToken = refTok
                                }, "Register Successful");
                            }
                        }

                        await _dbContext.SaveChangesAsync();
                        return Result<LoginResponseDto>.Success(new LoginResponseDto(), "Account updated. Please verify your email with the OTP.");
                    }

                    return Result<LoginResponseDto>.Failure("A user with this identifier already exists.");
                }
                #endregion

                OtpCode? newOtp = null;
                if (requiresEmailVerification)
                {
                    if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length < 6)
                    {
                        return Result<LoginResponseDto>.Failure("Invalid or expired OTP.");
                    }

                    newOtp = await _dbContext.OtpCodes
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefaultAsync(x => x.Destination == reqModel.Email && x.Channel == OtpChannels.Email && x.Purpose == OtpPurposes.VerifyEmail);

                    if (newOtp == null || newOtp.IsUsed || newOtp.RevokedAt != null || newOtp.ExpiresAt <= now || newOtp.FailedAttemptCount >= newOtp.MaxAttemptCount)
                    {
                        return Result<LoginResponseDto>.Failure("Invalid or expired OTP.");
                    }

                    if (_otpHasher.VerifyHashedPassword(newOtp, newOtp.CodeHash, otpCode) == PasswordVerificationResult.Failed)
                    {
                        newOtp.FailedAttemptCount++;
                        if (newOtp.FailedAttemptCount >= newOtp.MaxAttemptCount)
                        {
                            newOtp.RevokedAt = now;
                        }
                        await _dbContext.SaveChangesAsync();
                        return Result<LoginResponseDto>.Failure("Invalid or expired OTP.");
                    }

                    newOtp.IsUsed = true;
                    newOtp.UsedAt = now;
                }

                UserEntity user = new UserEntity();
                var hashedPassword = _passwordHasher.HashPasswordSafe(user, reqModel.Password!);

                var registerUser = new UserEntity
                {
                    Name = reqModel.Name!,
                    PhoneNumber = reqModel.PhoneNumber,
                    Password = hashedPassword,
                    RoleId = reqModel.RoleId,
                    Email = reqModel.Email,
                    ContactPhoneNumber = reqModel.ContactPhoneNumber,
                    CreatedAt = now,
                    EmailVerifiedAt = requiresEmailVerification || isAdminCreation || (reqModel.Email != null && !mode.RequireEmailVerification) ? now : null,
                    IsDeleted = false
                };

                await _dbContext.Users.AddAsync(registerUser);
                await _dbContext.SaveChangesAsync();

                // Create a profile for the selected Monk role.
                if (selectedRole.Name == SystemRoles.Monk)
                {
                    await _dbContext.MonkProfiles.AddAsync(new MonkProfile
                    {
                        UserId = registerUser.Id,
                        MonasteryName = reqModel.MonasteryName,
                        MonasteryAddress = reqModel.MonasteryAddress,
                    });
                    await _dbContext.SaveChangesAsync();
                }

                if (newOtp != null && newOtp.UserId == null)
                {
                    newOtp.UserId = registerUser.Id;
                    await _dbContext.SaveChangesAsync();
                }

                userToLogin = registerUser;

                var permissions = await _rolePermissionService.GetUserPermissions(userToLogin.Id);
                string token = _jwtOptions.GenerateJwtToken(userToLogin.Id, userToLogin.PhoneNumber ?? userToLogin.Email!, selectedRole.Name, permissions);
                string refreshToken = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = userToLogin.Id });

                return Result<LoginResponseDto>.Success(new LoginResponseDto
                {
                    AccessToken = token,
                    RoleId = userToLogin.RoleId,
                    RoleName = selectedRole.Name,
                    Permissions = permissions,
                    ID = userToLogin.Id,
                    RefreshToken = refreshToken
                }, "Register Successful");
            }
            catch (Exception ex)
            {
                return Result<LoginResponseDto>.Failure($"application layer err: {ex.Message} {ex.InnerException}");
            }
        }
        #endregion

        #region Login
        public async Task<Result<LoginResponseDto>> Login(LoginRequestDto reqModel)
        {
            try
            {
                var legacyPhoneNumberProvided = reqModel.Identifier is null && reqModel.PhoneNumber is not null;
                var identifier = reqModel.Identifier ?? reqModel.PhoneNumber;

                if (!identifier.TryNormalizeRequiredString(out var normalizedIdentifier)
                    || !reqModel.Password.TryNormalizeRequiredString(out _))
                {
                    return Result<LoginResponseDto>.Failure(GetInvalidCredentialsMessage(identifier, legacyPhoneNumberProvided));
                }

                identifier = normalizedIdentifier.ToLowerInvariant();
                var isEmail = identifier.Contains('@');

                var modeResult = await _loginModeService.GetActiveMode();
                if (modeResult.IsFailure || modeResult.Data is null)
                {
                    return Result<LoginResponseDto>.Failure(modeResult.Message);
                }

                var mode = modeResult.Data;
                if ((isEmail && mode.Code == LoginModeCodes.PhoneOnly) || (!isEmail && mode.Code == LoginModeCodes.EmailOnly))
                {
                    return Result<LoginResponseDto>.Failure(GetInvalidCredentialsMessage(identifier, legacyPhoneNumberProvided));
                }

                if ((!isEmail && !Regex.IsMatch(identifier, @"^\+?[0-9]+$")) || !Regex.IsMatch(reqModel.Password!, @"^[\x21-\x7E]+$"))
                {
                    return Result<LoginResponseDto>.Failure(GetInvalidCredentialsMessage(identifier, legacyPhoneNumberProvided));
                }

                var existingUser = await _dbContext.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(x => x.IsDeleted == false && (isEmail ? x.Email == identifier : x.PhoneNumber == identifier));

                if (existingUser == null || existingUser.Role == null || existingUser.Role.IsDeleted)
                {
                    return Result<LoginResponseDto>.Failure(GetInvalidCredentialsMessage(identifier, legacyPhoneNumberProvided));
                }

                UserEntity user = new UserEntity();
                if (_passwordHasher.VerifyPasswordSafe(user, existingUser.Password, reqModel.Password!)
                    == PasswordVerificationResult.Failed)
                {
                    return Result<LoginResponseDto>.Failure(GetInvalidCredentialsMessage(identifier, legacyPhoneNumberProvided));
                }

                var permissions = await _rolePermissionService.GetUserPermissions(existingUser.Id);

                if ((isEmail && mode.RequireEmailVerification && existingUser.EmailVerifiedAt is null)
                    || (!isEmail && mode.RequirePhoneVerification && existingUser.PhoneVerifiedAt is null))
                {
                    return Result<LoginResponseDto>.Failure("Identifier verification is required.");
                }

                string token = _jwtOptions.GenerateJwtToken(existingUser.Id, existingUser.PhoneNumber ?? existingUser.Email!, existingUser.Role.Name, permissions);
                string refreshToken = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = existingUser.Id });

                return Result<LoginResponseDto>.Success(new LoginResponseDto
                {
                    AccessToken = token,
                    RoleId = existingUser.RoleId,
                    RoleName = existingUser.Role.Name,
                    Permissions = permissions,
                    ID = existingUser.Id,
                    RefreshToken = refreshToken
                }, "Login Successful");
            }
            catch (Exception ex)
            {
                return Result<LoginResponseDto>.Failure($"Application layer err: {ex.Message} {ex.InnerException}");
            }
        }

        private static string GetInvalidCredentialsMessage(string? identifier, bool legacyPhoneNumberProvided)
        {
            var isEmail = identifier?.Contains('@') ?? false;
            if (isEmail && !legacyPhoneNumberProvided)
            {
                return "Invalid identifier or password.";
            }

            return "Phone number or password incorrect!";
        }
        #endregion

        #region ResetPassword
        public async Task<Result> ResetPassword(ResetPasswordRequestDto request)
        {
            try
            {
                var email = request.Email.NormalizeOptionalString()?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email) || !email.IsValidEmail())
                {
                    return Result.Failure("A valid email address is required.");
                }

                var otpCode = request.Otp.NormalizeOptionalString();
                if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length < 6)
                {
                    return Result.Failure("Invalid or expired OTP.");
                }

                if (!request.NewPassword.TryNormalizeRequiredString(out var normalizedPassword)
                    || normalizedPassword.Length < 6
                    || !Regex.IsMatch(normalizedPassword, @"^[\x21-\x7E]+$"))
                {
                    return Result.Failure("Password must be at least 6 characters and contain English characters and symbols only.");
                }

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsDeleted == false);
                if (user == null)
                {
                    return Result.Failure("Invalid request or expired OTP.");
                }

                var now = DateTime.UtcNow;
                var otp = await _dbContext.OtpCodes
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(x => x.Destination == email && x.Channel == OtpChannels.Email && x.Purpose == OtpPurposes.ResetPassword);

                if (otp == null || otp.IsUsed || otp.RevokedAt != null || otp.ExpiresAt <= now || otp.FailedAttemptCount >= otp.MaxAttemptCount)
                {
                    return Result.Failure("Invalid or expired OTP.");
                }

                if (_otpHasher.VerifyHashedPassword(otp, otp.CodeHash, otpCode) == PasswordVerificationResult.Failed)
                {
                    otp.FailedAttemptCount++;
                    if (otp.FailedAttemptCount >= otp.MaxAttemptCount)
                    {
                        otp.RevokedAt = now;
                    }
                    await _dbContext.SaveChangesAsync();
                    return Result.Failure("Invalid or expired OTP.");
                }

                otp.IsUsed = true;
                otp.UsedAt = now;

                user.Password = _passwordHasher.HashPasswordSafe(user, normalizedPassword);
                user.UpdatedAt = now;

                // Revoke all active refresh tokens for this user
                var activeTokens = await _dbContext.RefreshTokens
                    .Where(r => r.UserId == user.Id && r.RevokedAt == null)
                    .ToListAsync();
                foreach (var token in activeTokens)
                {
                    token.RevokedAt = now;
                }

                await _dbContext.SaveChangesAsync();

                return Result.Success("Password reset successful. Please log in with your new password.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application layer err: {ex.Message}");
            }
        }
        #endregion

        #region ChangePassword
        public async Task<Result> ChangePassword(int userId, ChangePasswordRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                {
                    return Result.Failure("Current password is required.");
                }

                var otpCode = request.Otp.NormalizeOptionalString();
                if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length < 6)
                {
                    return Result.Failure("Invalid or expired OTP.");
                }

                if (!request.NewPassword.TryNormalizeRequiredString(out var normalizedNewPassword)
                    || normalizedNewPassword.Length < 6
                    || !Regex.IsMatch(normalizedNewPassword, @"^[\x21-\x7E]+$"))
                {
                    return Result.Failure("New password must be at least 6 characters and contain English characters and symbols only.");
                }

                if (request.CurrentPassword == normalizedNewPassword)
                {
                    return Result.Failure("New password cannot be the same as the current password.");
                }

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == false);
                if (user == null)
                {
                    return Result.Failure("User not found.");
                }

                if (_passwordHasher.VerifyPasswordSafe(user, user.Password, request.CurrentPassword) == PasswordVerificationResult.Failed)
                {
                    return Result.Failure("Current password is incorrect.");
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return Result.Failure("User does not have a registered email address for verification.");
                }

                var now = DateTime.UtcNow;
                var otp = await _dbContext.OtpCodes
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(x => x.Destination == user.Email && x.Channel == OtpChannels.Email && x.Purpose == OtpPurposes.ChangePassword);

                if (otp == null || otp.IsUsed || otp.RevokedAt != null || otp.ExpiresAt <= now || otp.FailedAttemptCount >= otp.MaxAttemptCount)
                {
                    return Result.Failure("Invalid or expired OTP.");
                }

                if (_otpHasher.VerifyHashedPassword(otp, otp.CodeHash, otpCode) == PasswordVerificationResult.Failed)
                {
                    otp.FailedAttemptCount++;
                    if (otp.FailedAttemptCount >= otp.MaxAttemptCount)
                    {
                        otp.RevokedAt = now;
                    }
                    await _dbContext.SaveChangesAsync();
                    return Result.Failure("Invalid or expired OTP.");
                }

                otp.IsUsed = true;
                otp.UsedAt = now;

                user.Password = _passwordHasher.HashPasswordSafe(user, normalizedNewPassword);
                user.UpdatedAt = now;

                // Revoke all active refresh tokens for this user
                var activeTokens = await _dbContext.RefreshTokens
                    .Where(r => r.UserId == user.Id && r.RevokedAt == null)
                    .ToListAsync();
                foreach (var token in activeTokens)
                {
                    token.RevokedAt = now;
                }

                await _dbContext.SaveChangesAsync();

                return Result.Success("Password changed successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application layer err: {ex.Message}");
            }
        }
        #endregion

        #region RefreshTokens
        public async Task<Result<LoginResponseDto>> RefreshTokens(RefreshTokenRequestDto request)
        {
            try
            {
                var isValidRefreshToken = await this.IsValidRefreshToken(request.UserId, request.RefreshToken);

                if (isValidRefreshToken)
                {
                    var existingUser = await _dbContext.Users.Include(u => u.Role)
                        .FirstOrDefaultAsync(x => x.Id == request.UserId && x.IsDeleted == false);

                    if (existingUser == null || existingUser.Role == null || existingUser.Role.IsDeleted)
                    {
                        return Result<LoginResponseDto>.Failure("User not found");
                    }

                    var permissions = await _rolePermissionService.GetUserPermissions(existingUser.Id);
                    string token = _jwtOptions.GenerateJwtToken(existingUser.Id, existingUser.PhoneNumber ?? existingUser.Email!, existingUser.Role.Name, permissions);
                    string refreshToken = await this.GenerateAndSaveRefreshToken(new GenerateRefreshTokenDto { UserId = existingUser.Id });

                    return Result<LoginResponseDto>.Success(new LoginResponseDto
                    {
                        AccessToken = token,
                        RoleId = existingUser.RoleId,
                        RoleName = existingUser.Role.Name,
                        Permissions = permissions,
                        ID = existingUser.Id,
                        RefreshToken = refreshToken
                    }, "Successful");
                }
                else
                {
                    return Result<LoginResponseDto>.Failure("Invalid or expired Refresh Token");
                }
            }
            catch (Exception ex)
            {
                return Result<LoginResponseDto>.Failure($"application layer err: {ex.Message}");
            }
        }
        #endregion

        #region Logout
        public async Task<Result> Logout(int? userId, string? refreshToken)
        {
            try
            {
                int? targetUserId = userId is > 0 ? userId : null;
                var hasRefreshToken = refreshToken.TryNormalizeRequiredString(out var normalizedRefreshToken);

                if (!targetUserId.HasValue && !hasRefreshToken)
                {
                    return Result.Failure("User session or refresh token is required to logout.");
                }

                var now = DateTime.UtcNow;
                var tokensToRevoke = new List<RefreshToken>();

                if (hasRefreshToken)
                {
                    var hashedToken = normalizedRefreshToken.HashToken();
                    var tokenByHash = await _dbContext.RefreshTokens
                        .FirstOrDefaultAsync(r => r.RefreshToken1 == hashedToken);

                    if (tokenByHash != null)
                    {
                        if (targetUserId.HasValue && tokenByHash.UserId != targetUserId.Value)
                        {
                            return Result.Failure("Invalid refresh token for current user.");
                        }

                        if (tokenByHash.RevokedAt == null)
                        {
                            tokensToRevoke.Add(tokenByHash);
                        }
                    }
                }
                else if (targetUserId.HasValue)
                {
                    var uid = targetUserId.Value;
                    var userTokens = await _dbContext.RefreshTokens
                        .Where(r => r.UserId == uid && r.RevokedAt == null)
                        .ToListAsync();

                    tokensToRevoke.AddRange(userTokens);
                }

                foreach (var token in tokensToRevoke)
                {
                    token.RevokedAt = now;
                }

                if (tokensToRevoke.Count > 0)
                {
                    await _dbContext.SaveChangesAsync();
                }

                return Result.Success("Logged out successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Application layer err: {ex.Message}");
            }
        }
        #endregion

        #region GenerateAndSaveRefreshToken
        public async Task<string> GenerateAndSaveRefreshToken(GenerateRefreshTokenDto reqModel)
        {
            var refreshToken = 32.GenerateRefreshToken();
            var hashedRefreshToken = refreshToken.HashToken();

            reqModel.ExpiresAt = DateTime.UtcNow.AddDays(7);

            var existingRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.UserId == reqModel.UserId);

            if (existingRefreshToken == null)
            {
                await _dbContext.RefreshTokens.AddAsync(new RefreshToken
                {
                    UserId = reqModel.UserId,
                    TokenHash = hashedRefreshToken,
                    ExpiresAt = reqModel.ExpiresAt,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingRefreshToken.TokenHash = hashedRefreshToken;
                existingRefreshToken.ExpiresAt = reqModel.ExpiresAt;
                existingRefreshToken.RevokedAt = null;
            }

            await _dbContext.SaveChangesAsync();

            return refreshToken;
        }
        #endregion

        #region IsValidRefreshToken
        private async Task<bool> IsValidRefreshToken(int userId, string refreshToken)
        {
            if (!refreshToken.TryNormalizeRequiredString(out var normalizedRefreshToken))
            {
                return false;
            }

            var hashedRefreshToken = normalizedRefreshToken.HashToken();

            var tokenModel = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (tokenModel == null || tokenModel.TokenHash != hashedRefreshToken || tokenModel.ExpiresAt <= DateTime.UtcNow || tokenModel.RevokedAt != null)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
