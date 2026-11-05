using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HsumChaint.Shared;
using HsumChaint.Shared.CommonEnum;
using HsumChaint.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace HsumChaint.Tests;

public class DevCodeTests
{
    [Theory]
    [InlineData("  value  ", true, "value")]
    [InlineData("   ", false, "")]
    [InlineData(null, false, "")]
    public void RequiredStrings_AreValidatedAndNormalized(string? value, bool expected, string normalized)
    {
        Assert.Equal(expected, value.TryNormalizeRequiredString(out var actual));
        Assert.Equal(normalized, actual);
    }

    [Theory]
    [InlineData("  value  ", "value")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void OptionalStrings_AreNormalized(string? value, string? expected)
        => Assert.Equal(expected, value.NormalizeOptionalString());

    [Fact]
    public void NumberAndRangeValidation_HandlesBoundaries()
    {
        Assert.True(1.IsPositive());
        Assert.False(0.IsPositive());
        Assert.True(0m.IsNonNegative());
        Assert.False((-0.01m).IsNonNegative());
        Assert.True(((decimal?)null).IsNullOrNonNegative());
        Assert.True(10.IsInInclusiveRange(1, 10));
        Assert.False(11.IsInInclusiveRange(1, 10));
        Assert.False(5.IsInInclusiveRange(10, 1));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" user@example.com ", true)]
    [InlineData("not-an-email", false)]
    public void OptionalEmailValidation_AcceptsOnlyValidProvidedValues(string? email, bool expected)
        => Assert.Equal(expected, email.IsValidEmail());

    [Fact]
    public void EnumHelpers_RejectUndefinedValuesAndNormalizeSeparators()
    {
        Assert.True(DonationStatus.Completed.IsDefinedEnum());
        Assert.False(((DonationStatus)999).IsDefinedEnum());
        Assert.True("hsum-chaint".TryParseEnum<NotificationType>(out var type));
        Assert.Equal(NotificationType.Hsum_Chaint, type);
        Assert.False("999".TryParseEnum<NotificationType>(out _));
    }

    [Fact]
    public void DateRangeValidation_IsNullSafeAndInclusive()
    {
        var date = new DateTime(2026, 11, 5);
        Assert.True(((DateTime?)null).IsValidDateRange(date));
        Assert.True(((DateTime?)date).IsValidDateRange(date));
        Assert.False(((DateTime?)date.AddDays(1)).IsValidDateRange(date));
    }

    [Theory]
    [InlineData(1, 10, true, 0)]
    [InlineData(2, 10, true, 10)]
    [InlineData(0, 10, false, 0)]
    [InlineData(1, 0, false, 0)]
    [InlineData(int.MaxValue, int.MaxValue, false, 0)]
    public void PaginationOffset_IsValidatedWithoutOverflow(int page, int size, bool expected, int offset)
    {
        Assert.Equal(expected, page.TryGetPaginationOffset(size, out var actual));
        Assert.Equal(offset, actual);
    }

    [Fact]
    public void PasswordHelpers_UseIdentityCompatibleHashes()
    {
        var user = new PasswordTestUser();
        var hasher = new PasswordHasher<PasswordTestUser>();
        var hash = hasher.HashPasswordSafe(user, "Passw0rd!");

        Assert.NotEqual("Passw0rd!", hash);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            hasher.VerifyPasswordSafe(user, hash, "Passw0rd!"));
        Assert.Equal(PasswordVerificationResult.Failed,
            hasher.VerifyPasswordSafe(user, hash, "wrong-password"));
    }

    [Fact]
    public void JwtHelper_PreservesClaimsAndConfiguredLifetime()
    {
        var before = DateTime.UtcNow;
        var token = new JwtOptions
        {
            Issuer = "https://hsumchaint.local/",
            Audience = "https://hsumchaint.local/",
            Key = "ThisisTheSuperSecureKeyForOPOMProjectCalledHsumChaintAndThisKeyNeedsToBeAtLeast64Bytes",
            TokenValidityMins = 30
        }.GenerateJwtToken(
            42,
            "0912345678",
            "Admin",
            new[] { "Users.View", "Users.View", "Users.Manage" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("https://hsumchaint.local/", jwt.Issuer);
        Assert.Contains(jwt.Audiences, value => value == "https://hsumchaint.local/");
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == "42");
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        Assert.Equal(2, jwt.Claims.Count(claim => claim.Type == "permission"));
        Assert.InRange(jwt.ValidTo, before.AddMinutes(29), before.AddMinutes(31));
    }

    [Fact]
    public void RefreshTokens_AreRandomAndHashDeterministically()
    {
        var first = 32.GenerateRefreshToken();
        var second = 32.GenerateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(32, Convert.FromBase64String(first).Length);
        Assert.Equal(first.HashToken(), first.HashToken());
        Assert.NotEqual(first, first.HashToken());
    }

    private sealed class PasswordTestUser;
}
