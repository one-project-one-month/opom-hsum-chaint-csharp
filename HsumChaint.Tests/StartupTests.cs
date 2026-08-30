using HsumChaint.Database.Models;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
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
                        options.UseInMemoryDatabase("HsumChaintApiTests"));
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
    }
}
