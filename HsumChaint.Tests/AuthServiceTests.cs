using HsumChaint.Domain.Features.RolePermission.Services;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Domain.Features.Auth.Services;
using HsumChaint.Shared.CommonEnum;
using HsumChaint.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using InfrastructureUser = HsumChaint.Database.Models.User;

namespace HsumChaint.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAndRefresh_LoadActivePermissionClaimsFromDatabase()
    {
        await using var db = CreateDbContext();
        var user = CreateUser("09100000002", "Passw0rd!");
        user.RoleId = 1;
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var login = await service.Login(new() { PhoneNumber = user.PhoneNumber, Password = "Passw0rd!" });
        Assert.True(login.IsSuccess);
        Assert.Equal("Admin", login.Data!.RoleName);
        Assert.Equal(HsumChaint.Shared.Authorization.Permissions.All.OrderBy(p => p), login.Data.Permissions);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(login.Data.AccessToken);
        Assert.Equal(login.Data.Permissions, jwt.Claims.Where(c => c.Type == "permission").Select(c => c.Value));
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());

        db.RolePermissions.First(rp => rp.RoleId == 1 && rp.PermissionId == 1).IsDeleted = true;
        db.Permissions.Find(2)!.IsDeleted = true;
        await db.SaveChangesAsync();
        var refreshed = await service.RefreshTokens(new() { UserId = user.Id, RefreshToken = login.Data.RefreshToken });
        Assert.True(refreshed.IsSuccess);
        Assert.Equal(13, refreshed.Data!.Permissions.Count);
        Assert.DoesNotContain("Roles.View", refreshed.Data.Permissions);
        Assert.DoesNotContain("Roles.Manage", refreshed.Data.Permissions);
        jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(refreshed.Data.AccessToken);
        Assert.Equal(refreshed.Data.Permissions, jwt.Claims.Where(c => c.Type == "permission").Select(c => c.Value));
    }

    [Fact]
    public async Task Register_ValidatesRole_AndCreatesMonkProfileWithoutMonasteryDetails()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var request = new RegisterRequestDto { Name = "Monk", PhoneNumber = "091", Password = "Passw0rd!", RoleId = 999 };
        Assert.False((await service.Register(request)).IsSuccess);
        Assert.Empty(db.Users);
        request.RoleId = 2;
        Assert.True((await service.Register(request)).IsSuccess);
        Assert.Equal(2, db.Users.Single().RoleId);
        Assert.Equal(db.Users.Single().Id, db.MonkProfiles.Single().UserId);
        Assert.Equal("Passw0rd!", request.Password);
    }

    [Fact]
    public async Task LoginAndRefresh_RejectInactiveRole()
    {
        await using var db = CreateDbContext();
        var user = CreateUser("091", "Passw0rd!");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var login = await service.Login(new() { PhoneNumber = "091", Password = "Passw0rd!" });
        db.Roles.Find(3)!.IsDeleted = true;
        await db.SaveChangesAsync();
        Assert.False((await service.Login(new() { PhoneNumber = "091", Password = "Passw0rd!" })).IsSuccess);
        Assert.False((await service.RefreshTokens(new() { UserId = user.Id, RefreshToken = login.Data!.RefreshToken })).IsSuccess);
    }

    [Fact]
    public async Task Register_CreatesUser_WhenPhoneNumberIsNew()
    {
        await using var dbContext = CreateDbContext();
        var authService = CreateService(dbContext);

        var response = await authService.Register(new RegisterRequestDto
        {
            Name = "Test User",
            PhoneNumber = "1234567890",
            Password = "Passw0rd!",
            RoleId = 3,
            Email = "test@hsumchaint.local"
        });

        Assert.True(response.IsSuccess);
        Assert.Equal("Register Successful", response.Message);
        Assert.NotNull(await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == "1234567890"));
    }

    [Fact]
    public async Task Login_ReturnsAccessAndRefreshToken_WhenCredentialsAreValid()
    {
        await using var dbContext = CreateDbContext();
        var request = new LoginRequestDto
        {
            PhoneNumber = "1234567890",
            Password = "Passw0rd!"
        };

        dbContext.Users.Add(CreateUser(request.PhoneNumber!, request.Password!));
        await dbContext.SaveChangesAsync();
        var authService = CreateService(dbContext);

        var response = await authService.Login(request);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.False(string.IsNullOrWhiteSpace(response.Data?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.Data?.RefreshToken));
        Assert.Equal(3, response.Data!.RoleId);
        Assert.NotNull(await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == response.Data.ID));
    }

    [Fact]
    public async Task RefreshTokens_ReturnsNewTokens_WhenRefreshTokenIsValid()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("1234567890", "Passw0rd!");
        user.Id = 55;
        var currentRefreshToken = Guid.NewGuid().ToString("N");
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedToken = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(currentRefreshToken)));

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            RefreshToken1 = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await dbContext.SaveChangesAsync();
        var authService = CreateService(dbContext);

        var response = await authService.RefreshTokens(new RefreshTokenRequestDto
        {
            UserId = user.Id,
            RefreshToken = currentRefreshToken
        });

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.False(string.IsNullOrWhiteSpace(response.Data?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.Data?.RefreshToken));
        Assert.Equal(user.Id, response.Data?.ID);
    }

    [Fact]
    public async Task RefreshTokens_RejectsRevokedRefreshToken()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("1234567890", "Passw0rd!");
        user.Id = 56;
        var currentRefreshToken = Guid.NewGuid().ToString("N");
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedToken = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(currentRefreshToken)));

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            RefreshToken1 = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RevokedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var authService = CreateService(dbContext);

        var response = await authService.RefreshTokens(new RefreshTokenRequestDto
        {
            UserId = user.Id,
            RefreshToken = currentRefreshToken
        });

        Assert.False(response.IsSuccess);
        Assert.Equal("Invalid or expired Refresh Token", response.Message);
    }

    [Fact]
    public async Task GenerateAndSaveRefreshToken_ClearsRevokedAt_WhenTokenReplaced()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("1234567890", "Passw0rd!");
        user.Id = 57;

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            RefreshToken1 = "old_hashed_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RevokedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var authService = CreateService(dbContext);

        var newRawToken = await authService.GenerateAndSaveRefreshToken(new() { UserId = user.Id });

        var tokenRecord = await dbContext.RefreshTokens.SingleAsync(r => r.UserId == user.Id);
        Assert.Null(tokenRecord.RevokedAt);
        Assert.NotEqual(newRawToken, tokenRecord.RefreshToken1);

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(newRawToken)));
        Assert.Equal(expectedHash, tokenRecord.RefreshToken1);
    }

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
                Issuer = "https://hsumchaint.local/",
                Audience = "https://hsumchaint.local/",
                Key = "ThisisTheSuperSecureKeyForOPOMProjectCalledHsumChaintAndThisKeyNeedsToBeAtLeast64BytesBecuaseItUsesHmacSha512"
            }), new RolePermissionService(dbContext));
    }

    private static InfrastructureUser CreateUser(string phoneNumber, string password)
    {
        var user = new InfrastructureUser
        {
            Id = 1,
            PhoneNumber = phoneNumber,
            Name = "Test User",
            RoleId = 3,
            Email = "test@hsumchaint.local",
            IsDeleted = false
        };

        user.Password = new PasswordHasher<InfrastructureUser>().HashPassword(user, password);
        return user;
    }
}
