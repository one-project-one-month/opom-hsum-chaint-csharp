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
    public async Task Register_CreatesUser_WhenPhoneNumberIsNew()
    {
        await using var dbContext = CreateDbContext();
        var authService = CreateService(dbContext);

        var response = await authService.Register(new RegisterRequestDto
        {
            Name = "Test User",
            PhoneNumber = "1234567890",
            Password = "Passw0rd!",
            UserType = UserType.User,
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
        Assert.Equal(UserType.User, response.Data!.UserType);
        Assert.NotNull(await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == response.Data.ID));
    }

    [Fact]
    public async Task RefreshTokens_ReturnsNewTokens_WhenRefreshTokenIsValid()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("1234567890", "Passw0rd!");
        user.Id = 55;
        var currentRefreshToken = Guid.NewGuid().ToString("N");

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            RefreshToken1 = currentRefreshToken,
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

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
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
            }));
    }

    private static InfrastructureUser CreateUser(string phoneNumber, string password)
    {
        var user = new InfrastructureUser
        {
            Id = 1,
            PhoneNumber = phoneNumber,
            Name = "Test User",
            UserType = UserType.User,
            Email = "test@hsumchaint.local",
            IsDeleted = false
        };

        user.Password = new PasswordHasher<InfrastructureUser>().HashPassword(user, password);
        return user;
    }
}
