using HsumChaint.Shared;
using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Domain.Features.Donation.ServiceInterfaces;
using HsumChaint.Domain.Features.Monastery.ServiceInterfaces;
using HsumChaint.Domain.Features.Notification.ServiceInterfaces;
using HsumChaint.Domain.Features.User.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HsumChaint.Tests;

public class StartupTests
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public StartupTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(_databaseName));
                });
            });
    }

    [Fact]
    public void Startup_ResolvesFeatureDomainServices()
    {
        using var scope = _factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        Assert.NotNull(serviceProvider.GetRequiredService<IAuthService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IUserService>());
        Assert.NotNull(serviceProvider.GetRequiredService<INotificationService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IMonasteryService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IDonationService>());
        Assert.NotNull(serviceProvider.GetRequiredService<HsumChaint.Domain.Features.RolePermission.ServiceInterfaces.IRolePermissionService>());
    }

    [Fact]
    public void Startup_ConfiguresControllerAndOpenApiRoutes()
    {
        var endpoints = _factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints.OfType<RouteEndpoint>())
            .ToList();

        Assert.Contains(endpoints, x => x.RoutePattern.RawText?.Contains("api/", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(
            endpoints,
            x => x.RoutePattern.RawText?.Contains("openapi", StringComparison.OrdinalIgnoreCase) == true ||
                 x.DisplayName?.Contains("openapi", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Startup_OpenApiEndpoint_ReturnsInDevelopment()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var bearer = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
        Assert.True(document.RootElement.GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task Startup_RootRedirectsToScalar_InDevelopment()
    {
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var response = await client.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/scalar/v1", response.Headers.Location?.OriginalString);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/scalar/v1")).StatusCode);
    }

    [Fact]
    public async Task Startup_DonationAndMonasteryEndpoints_RequireAuthentication()
    {
        using var client = _factory.CreateClient();

        var donationsResponse = await client.GetAsync("/api/v1/donations");
        var monasteriesResponse = await client.GetAsync("/api/v1/monasteries/mine");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, donationsResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, monasteriesResponse.StatusCode);
    }

    [Fact]
    public async Task Rbac_LoginWithSeededAdminHash_GrantsProtectedApiAccess()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        RbacTestData.Seed(db);
        db.Users.Add(new User
        {
            Id = 2, Name = "Ko Admin", PhoneNumber = "09100000002", RoleId = 1, IsDeleted = false,
            Password = "AQAAAAIAAYagAAAAEDIpV3urEmFsT/sadx0glQu6ZQVPR2rBoaOrdj3OBmc0hdVzfIYOYHh972IzaIUUlg=="
        });
        await db.SaveChangesAsync();
        using var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/v1/auth/login",
            new { PhoneNumber = "09100000002", Password = "Passw0rd!" });
        Assert.Equal(System.Net.HttpStatusCode.OK, login.StatusCode);
        var response = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<HsumChaint.Shared.Result<HsumChaint.Domain.Features.Auth.DTOs.LoginResponseDto>>(login.Content);
        Assert.Equal(15, response!.Data!.Permissions.Count);
        client.DefaultRequestHeaders.Authorization = new("Bearer", response.Data.AccessToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/api/v1/roles")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/api/v1/permissions")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/api/User")).StatusCode);
        var permissionPageResponse = await client.GetAsync("/api/v1/permissions?pageNumber=2&pageSize=3");
        Assert.Equal(System.Net.HttpStatusCode.OK, permissionPageResponse.StatusCode);
        using var permissionPage = System.Text.Json.JsonDocument.Parse(await permissionPageResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, permissionPage.RootElement.GetProperty("data").GetArrayLength());
        Assert.False(permissionPage.RootElement.TryGetProperty("listData", out _));
        var pagination = permissionPage.RootElement.GetProperty("pagination");
        Assert.Equal(2, pagination.GetProperty("pageNumber").GetInt32());
        Assert.Equal(3, pagination.GetProperty("pageSize").GetInt32());
        Assert.Equal(15, pagination.GetProperty("totalCount").GetInt32());
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await client.GetAsync("/api/User?pageNumber=0")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync("/api/User/999")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/donations?monasterySpaceId=999")).StatusCode);
        var create = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/v1/roles",
            new { Name = "Custom API Reviewer", PermissionIds = new[] { 13 } });
        Assert.Equal(System.Net.HttpStatusCode.OK, create.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/v1/roles/1")).StatusCode);
    }

    [Fact]
    public async Task Rbac_AdminWithoutPermissionMapping_IsForbidden_AndRegistrationIsAllowedForAnonymous()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        RbacTestData.Seed(db);
        db.RolePermissions.RemoveRange(db.RolePermissions.Where(rp => rp.RoleId == 1));
        var admin = new User { Name = "Admin", PhoneNumber = "091", RoleId = 1, IsDeleted = false };
        admin.Password = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(admin, "Passw0rd!");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        using var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var anonymousRegister = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/v1/auth/register",
            new { Name = "Self Admin", PhoneNumber = "092", Password = "Passw0rd!", RoleId = 1 });
        Assert.Equal(System.Net.HttpStatusCode.OK, anonymousRegister.StatusCode);
        var login = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/v1/auth/login",
            new { PhoneNumber = "091", Password = "Passw0rd!" });
        var response = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<HsumChaint.Shared.Result<HsumChaint.Domain.Features.Auth.DTOs.LoginResponseDto>>(login.Content);
        Assert.Empty(response!.Data!.Permissions);
        client.DefaultRequestHeaders.Authorization = new("Bearer", response.Data.AccessToken);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/roles")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/permissions")).StatusCode);
    }
}
