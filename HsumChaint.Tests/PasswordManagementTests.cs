using System;
using System.Linq;
using System.Threading.Tasks;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.Services;
using HsumChaint.Domain.Features.RolePermission.Services;
using HsumChaint.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using InfrastructureUser = HsumChaint.Database.Models.User;

namespace HsumChaint.Tests;

public class PasswordManagementTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        RbacTestData.Seed(context);
        return context;
    }

    private static AuthService CreateService(AppDbContext dbContext)
    {
        return new AuthService(
            dbContext,
            new PasswordHasher<InfrastructureUser>(),
            Options.Create(new JwtOptions
            {
                Key = "ThisisTheSuperSecureKeyForOPOMProjectCalledHsumChaintAndThisKeyNeedsToBeAtLeast64BytesBecuaseItUsesHmacSha512",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                TokenValidityMins = 30
            }),
            new RolePermissionService(dbContext),
            new DefaultTestLoginModeService(),
            new PasswordHasher<OtpCode>());
    }

    private sealed class DefaultTestLoginModeService : HsumChaint.Domain.Features.Auth.ServiceInterfaces.ILoginModeService
    {
        public Task<HsumChaint.Shared.Result<LoginMode>> GetActiveMode() =>
            Task.FromResult(HsumChaint.Shared.Result<LoginMode>.Success(new LoginMode
            {
                Code = LoginModeCodes.PhoneOrEmail,
                IsActive = true,
                RequireEmailVerification = true
            }));

        public Task<HsumChaint.Shared.Result<System.Collections.Generic.IReadOnlyList<LoginModeDto>>> GetAllModes() =>
            throw new NotSupportedException();

        public Task<HsumChaint.Shared.Result> UpdateActiveMode(int adminUserId, UpdateLoginModeRequestDto request) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task ResetPassword_Succeeds_AndRevokesTokens_WhenOtpAndNewPasswordAreValid()
    {
        await using var db = CreateDbContext();
        var hasher = new PasswordHasher<InfrastructureUser>();
        var user = new InfrastructureUser
        {
            Id = 101,
            Name = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "0911111111",
            RoleId = 3,
            IsDeleted = false,
            EmailVerifiedAt = DateTime.UtcNow
        };
        user.Password = hasher.HashPassword(user, "OldPassword123!");
        db.Users.Add(user);

        // Add active refresh token
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            TokenHash = "old_token_hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        // Add valid OTP
        var otpHasher = new PasswordHasher<OtpCode>();
        var otp = new OtpCode
        {
            Id = 1001,
            UserId = user.Id,
            Destination = user.Email,
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            FailedAttemptCount = 0,
            MaxAttemptCount = 5,
            CreatedAt = DateTime.UtcNow
        };
        otp.CodeHash = otpHasher.HashPassword(otp, "123456");
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ResetPassword(new ResetPasswordRequestDto(user.Email, "123456", "NewPassword123!"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Password reset successful. Please log in with your new password.", result.Message);

        // Verify password updated
        var updatedUser = await db.Users.FindAsync(user.Id);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(updatedUser!, updatedUser!.Password, "NewPassword123!"));

        // Verify OTP marked used
        var updatedOtp = await db.OtpCodes.FindAsync(otp.Id);
        Assert.True(updatedOtp!.IsUsed);

        // Verify refresh token revoked
        var token = await db.RefreshTokens.FirstAsync(r => r.UserId == user.Id);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task ResetPassword_Fails_WhenOtpIsInvalid()
    {
        await using var db = CreateDbContext();
        var user = new InfrastructureUser
        {
            Id = 102,
            Name = "Jane Doe",
            Email = "jane@example.com",
            PhoneNumber = "0922222222",
            RoleId = 3,
            Password = "OldPassword123!",
            IsDeleted = false
        };
        db.Users.Add(user);

        var otpHasher = new PasswordHasher<OtpCode>();
        var otp = new OtpCode
        {
            Id = 1002,
            Destination = user.Email,
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            FailedAttemptCount = 0,
            MaxAttemptCount = 5,
            CreatedAt = DateTime.UtcNow
        };
        otp.CodeHash = otpHasher.HashPassword(otp, "123456");
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ResetPassword(new ResetPasswordRequestDto(user.Email, "654321", "NewPassword123!"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired OTP.", result.Message);
    }

    [Fact]
    public async Task ResetPassword_Fails_WhenOtpIsExpired()
    {
        await using var db = CreateDbContext();
        var user = new InfrastructureUser
        {
            Id = 103,
            Name = "Bob Doe",
            Email = "bob@example.com",
            PhoneNumber = "0933333333",
            RoleId = 3,
            Password = "OldPassword123!",
            IsDeleted = false
        };
        db.Users.Add(user);

        var otpHasher = new PasswordHasher<OtpCode>();
        var otp = new OtpCode
        {
            Id = 1003,
            Destination = user.Email,
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            IsUsed = false,
            FailedAttemptCount = 0,
            MaxAttemptCount = 5,
            CreatedAt = DateTime.UtcNow.AddMinutes(-6)
        };
        otp.CodeHash = otpHasher.HashPassword(otp, "123456");
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ResetPassword(new ResetPasswordRequestDto(user.Email, "123456", "NewPassword123!"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired OTP.", result.Message);
    }

    [Fact]
    public async Task ChangePassword_Succeeds_WhenCurrentPasswordAndOtpAreValid()
    {
        await using var db = CreateDbContext();
        var hasher = new PasswordHasher<InfrastructureUser>();
        var user = new InfrastructureUser
        {
            Id = 104,
            Name = "Alice Doe",
            Email = "alice@example.com",
            PhoneNumber = "0944444444",
            RoleId = 3,
            IsDeleted = false
        };
        user.Password = hasher.HashPassword(user, "CurrentPassword123!");
        db.Users.Add(user);

        // Add active refresh token
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = 2,
            UserId = user.Id,
            TokenHash = "alice_token_hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        // Add valid OTP for change password
        var otpHasher = new PasswordHasher<OtpCode>();
        var otp = new OtpCode
        {
            Id = 1004,
            UserId = user.Id,
            Destination = user.Email,
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.ChangePassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            FailedAttemptCount = 0,
            MaxAttemptCount = 5,
            CreatedAt = DateTime.UtcNow
        };
        otp.CodeHash = otpHasher.HashPassword(otp, "999888");
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ChangePassword(user.Id, new ChangePasswordRequestDto("CurrentPassword123!", "BrandNewPassword123!", "999888"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Password changed successfully.", result.Message);

        // Verify password updated
        var updatedUser = await db.Users.FindAsync(user.Id);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(updatedUser!, updatedUser!.Password, "BrandNewPassword123!"));

        // Verify tokens revoked
        var token = await db.RefreshTokens.FirstAsync(r => r.UserId == user.Id);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task ChangePassword_Fails_WhenCurrentPasswordIsWrong()
    {
        await using var db = CreateDbContext();
        var hasher = new PasswordHasher<InfrastructureUser>();
        var user = new InfrastructureUser
        {
            Id = 105,
            Name = "Tom Doe",
            Email = "tom@example.com",
            PhoneNumber = "0955555555",
            RoleId = 3,
            IsDeleted = false
        };
        user.Password = hasher.HashPassword(user, "CorrectPassword123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ChangePassword(user.Id, new ChangePasswordRequestDto("WrongPassword123!", "NewPassword123!", "999888"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Current password is incorrect.", result.Message);
    }

    [Fact]
    public async Task ChangePassword_Fails_WhenNewPasswordMatchesCurrentPassword()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var result = await service.ChangePassword(1, new ChangePasswordRequestDto("SamePassword123!", "SamePassword123!", "999888"));

        Assert.False(result.IsSuccess);
        Assert.Equal("New password cannot be the same as the current password.", result.Message);
    }

    [Fact]
    public async Task Register_UpdatesUnverifiedUser_WhenSameEmailReRegisters()
    {
        await using var db = CreateDbContext();
        var existingUnverifiedUser = new InfrastructureUser
        {
            Id = 106,
            Name = "Initial Abandoned",
            Email = "pending@example.com",
            PhoneNumber = "0966666666",
            Password = "OldHashedPassword",
            RoleId = 3,
            EmailVerifiedAt = null, // Not verified!
            IsDeleted = false
        };
        db.Users.Add(existingUnverifiedUser);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.Register(new RegisterRequestDto
        {
            Name = "Updated Name",
            Email = "pending@example.com",
            PhoneNumber = "0977777777",
            Password = "NewStrongPassword123!",
            RoleId = 3
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Account updated. Please verify your email with the OTP.", result.Message);

        var userInDb = await db.Users.FindAsync(existingUnverifiedUser.Id);
        Assert.Equal("Updated Name", userInDb!.Name);
        Assert.Equal("0977777777", userInDb.PhoneNumber);
    }

    [Fact]
    public async Task Register_Rejects_WhenUserIsAlreadyVerified()
    {
        await using var db = CreateDbContext();
        var existingVerifiedUser = new InfrastructureUser
        {
            Id = 107,
            Name = "Verified User",
            Email = "verified@example.com",
            PhoneNumber = "0988888888",
            Password = "Password123!",
            RoleId = 3,
            EmailVerifiedAt = DateTime.UtcNow, // Already verified!
            IsDeleted = false
        };
        db.Users.Add(existingVerifiedUser);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.Register(new RegisterRequestDto
        {
            Name = "Someone Else",
            Email = "verified@example.com",
            PhoneNumber = "0999999999",
            Password = "Password123!",
            RoleId = 3
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("A user with this identifier already exists.", result.Message);
    }

    [Fact]
    public async Task Register_WithValidOtp_WhenEmailVerificationRequired_SucceedsAndReturnsTokens()
    {
        await using var db = CreateDbContext();
        var otpHasher = new PasswordHasher<OtpCode>();
        var otp = new OtpCode
        {
            Id = 9001,
            Destination = "newuser@example.com",
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.VerifyEmail,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            FailedAttemptCount = 0,
            MaxAttemptCount = 5,
            CreatedAt = DateTime.UtcNow
        };
        otp.CodeHash = otpHasher.HashPassword(otp, "654321");
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.Register(new RegisterRequestDto
        {
            Name = "New Verified User",
            Email = "newuser@example.com",
            PhoneNumber = "0911112222",
            Password = "Password123!",
            RoleId = 3,
            Otp = "654321"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Register Successful", result.Message);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));

        var createdUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        Assert.NotNull(createdUser);
        Assert.NotNull(createdUser!.EmailVerifiedAt);

        var updatedOtp = await db.OtpCodes.FindAsync(otp.Id);
        Assert.True(updatedOtp!.IsUsed);
        Assert.Equal(createdUser.Id, updatedOtp.UserId);
    }

    [Fact]
    public async Task Register_WithInvalidOtp_WhenEmailVerificationRequired_FailsAndDoesNotCreateUser()
    {
        await using var db = CreateDbContext();
        var otpHasher = new PasswordHasher<OtpCode>();
        var otp = new OtpCode
        {
            Id = 9002,
            Destination = "failuser@example.com",
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.VerifyEmail,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            FailedAttemptCount = 0,
            MaxAttemptCount = 5,
            CreatedAt = DateTime.UtcNow
        };
        otp.CodeHash = otpHasher.HashPassword(otp, "654321");
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.Register(new RegisterRequestDto
        {
            Name = "Fail User",
            Email = "failuser@example.com",
            PhoneNumber = "0933334444",
            Password = "Password123!",
            RoleId = 3,
            Otp = "000000" // Wrong code
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired OTP.", result.Message);
        Assert.Null(result.Data);

        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.Email == "failuser@example.com");
        Assert.Null(userInDb);
    }

    [Fact]
    public async Task Register_WithMissingOtp_WhenEmailVerificationRequired_Fails()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var result = await service.Register(new RegisterRequestDto
        {
            Name = "No Otp User",
            Email = "nootp@example.com",
            PhoneNumber = "0955556666",
            Password = "Password123!",
            RoleId = 3,
            Otp = null
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired OTP.", result.Message);
    }

    [Fact]
    public async Task OtpService_Request_AllowsNewEmail_AndRejectsAlreadyRegisteredVerifiedEmail()
    {
        await using var db = CreateDbContext();
        var existingUser = new InfrastructureUser
        {
            Id = 501,
            Name = "Existing Verified",
            Email = "alreadyexists@example.com",
            PhoneNumber = "0977889900",
            Password = "Password123!",
            RoleId = 3,
            EmailVerifiedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();

        var otpService = new OtpService(
            db,
            new DefaultTestLoginModeService(),
            new TestEmailSender(),
            new PasswordHasher<OtpCode>(),
            Options.Create(new HsumChaint.Domain.Configuration.OtpOptions()),
            new TestMediator());

        // 1. Should reject if email is already verified
        var rejectResult = await otpService.Request(new RequestOtpDto
        {
            Destination = "alreadyexists@example.com",
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.VerifyEmail
        });
        Assert.False(rejectResult.IsSuccess);
        Assert.Equal("A user with this email is already registered.", rejectResult.Message);

        // 2. Should succeed for new unregistered email
        var successResult = await otpService.Request(new RequestOtpDto
        {
            Destination = "brandnewemail@example.com",
            Channel = OtpChannels.Email,
            Purpose = OtpPurposes.VerifyEmail
        });
        Assert.True(successResult.IsSuccess);
        Assert.Equal("OTP has been sent.", successResult.Message);
        Assert.NotNull(await db.OtpCodes.FirstOrDefaultAsync(o => o.Destination == "brandnewemail@example.com"));
    }

    private sealed class TestEmailSender : HsumChaint.Domain.Features.Auth.ServiceInterfaces.IEmailSender
    {
        public Task SendOtpAsync(string email, string otp, DateTime expiresAt, string? purpose = null) => Task.CompletedTask;
    }

    private sealed class TestMediator : MediatR.IMediator
    {
        public Task Publish(object notification, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, System.Threading.CancellationToken cancellationToken = default) where TNotification : MediatR.INotification => Task.CompletedTask;
        public Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Send<TRequest>(TRequest request, System.Threading.CancellationToken cancellationToken = default) where TRequest : MediatR.IRequest => throw new NotSupportedException();
        public Task<object?> Send(object request, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, System.Threading.CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
