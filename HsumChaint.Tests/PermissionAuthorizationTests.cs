using System.Security.Claims;
using HsumChaint.API.Authorization;
using HsumChaint.API.Extensions;
using HsumChaint.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HsumChaint.Tests;

public class PermissionAuthorizationTests
{
    [Theory]
    [InlineData("Admin", null, false)]
    [InlineData("Custom Role", "Donation.Review", true)]
    [InlineData("Admin", "Donation.View", false)]
    [InlineData("Custom Role", "donation.review", false)]
    public async Task Authorization_RequiresExactPermissionClaim(string role, string? permission, bool allowed)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (permission != null) claims.Add(new Claim("permission", permission));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var requirement = new PermissionRequirement(Permissions.Donation.Review);
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, null);
        await new PermissionAuthorizationHandler().HandleAsync(context);
        Assert.Equal(allowed, context.HasSucceeded);
    }

    [Fact]
    public async Task Authorization_RejectsUnauthenticatedIdentityWithPermission()
    {
        var context = new AuthorizationHandlerContext(new[] { new PermissionRequirement(Permissions.Roles.Manage) },
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", Permissions.Roles.Manage) })), null);
        await new PermissionAuthorizationHandler().HandleAsync(context);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task AllPermissionPolicies_AreRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiServices();
        await using var provider = services.BuildServiceProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        foreach (var permission in Permissions.All)
        {
            var policy = await policies.GetPolicyAsync(permission);
            Assert.NotNull(policy);
            Assert.Contains(policy.Requirements, r => r is PermissionRequirement pr && pr.Permission == permission);
        }
    }
}
