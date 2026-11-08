using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.DTOs;
using HsumChaint.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HsumChaint.Tests;

public class AuthLogoutEndpointTests
{
    [Fact]
    public async Task LogoutEndpoint_RevokesToken_ThroughHttpPipeline()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        RbacTestData.Seed(db);
        var user = new User { Id = 201, Name = "Logout User", PhoneNumber = "09100000201", RoleId = 3, IsDeleted = false };
        user.Password = new PasswordHasher<User>().HashPassword(user, "Test-password-only");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        // 1. Call logout with neither token nor body -> 400 Bad Request
        var emptyLogout = await client.PostAsJsonAsync("/api/v1/Auth/logout", new LogoutRequestDto());
        Assert.Equal(HttpStatusCode.BadRequest, emptyLogout.StatusCode);

        // 2. Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/Auth/login", new { PhoneNumber = user.PhoneNumber, Password = "Test-password-only" });
        var login = await loginResponse.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        Assert.True(login!.IsSuccess);
        var refreshToken = login.Data!.RefreshToken;
        var accessToken = login.Data!.AccessToken;

        // 3. Call logout with Bearer token & refresh token body -> 200 OK
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        var logoutResponse = await client.PostAsJsonAsync("/api/v1/Auth/logout", new LogoutRequestDto { RefreshToken = refreshToken });
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // 4. Verify refresh token is now revoked
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/Auth/refresh-token", new RefreshTokenRequestDto { UserId = user.Id, RefreshToken = refreshToken });
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        Assert.False(refreshResult!.IsSuccess);

        // 5. Test resilient logout: login again, and logout with NO Bearer token, passing ONLY refreshToken in body
        var loginResponse2 = await client.PostAsJsonAsync("/api/v1/Auth/login", new { PhoneNumber = user.PhoneNumber, Password = "Test-password-only" });
        var login2 = await loginResponse2.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        Assert.True(login2!.IsSuccess);

        client.DefaultRequestHeaders.Authorization = null; // Unauthenticated client
        var logoutResponse2 = await client.PostAsJsonAsync("/api/v1/Auth/logout", new LogoutRequestDto { RefreshToken = login2.Data!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, logoutResponse2.StatusCode);

        // 6. Verify second refresh token was also revoked
        var refreshResponse2 = await client.PostAsJsonAsync("/api/v1/Auth/refresh-token", new RefreshTokenRequestDto { UserId = user.Id, RefreshToken = login2.Data!.RefreshToken });
        var refreshResult2 = await refreshResponse2.Content.ReadFromJsonAsync<Result<LoginResponseDto>>();
        Assert.False(refreshResult2!.IsSuccess);
    }
}
