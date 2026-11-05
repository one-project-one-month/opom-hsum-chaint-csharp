using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HsumChaint.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace HsumChaint.Shared;

public static class DevCode
{
    public static bool TryNormalizeRequiredString(this string? value, out string normalizedValue)
    {
        normalizedValue = value?.Trim() ?? string.Empty;
        return normalizedValue.Length > 0;
    }

    public static string? NormalizeOptionalString(this string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrEmpty(normalizedValue) ? null : normalizedValue;
    }

    public static bool IsPositive(this int value)
    {
        return value > 0;
    }

    public static bool IsNonNegative(this decimal value)
    {
        return value >= 0;
    }

    public static bool IsNullOrNonNegative(this decimal? value)
    {
        return !value.HasValue || value.Value.IsNonNegative();
    }

    public static bool IsInInclusiveRange<T>(this T value, T minimum, T maximum)
        where T : IComparable<T>
    {
        return minimum.CompareTo(maximum) <= 0
            && value.CompareTo(minimum) >= 0
            && value.CompareTo(maximum) <= 0;
    }

    public static bool IsValidOptionalEmail(this string? email)
    {
        var normalizedEmail = email.NormalizeOptionalString();
        if (normalizedEmail == null)
        {
            return true;
        }

        try
        {
            var address = new MailAddress(normalizedEmail);
            return string.Equals(address.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool IsDefinedEnum<TEnum>(this TEnum value)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(value);
    }

    public static bool TryParseEnum<TEnum>(
        this string? value,
        out TEnum result,
        bool ignoreCase = true,
        bool normalizeSeparators = true)
        where TEnum : struct, Enum
    {
        var normalizedValue = value.NormalizeOptionalString();
        if (normalizedValue == null)
        {
            result = default;
            return false;
        }

        if (normalizeSeparators)
        {
            normalizedValue = normalizedValue.Replace('-', '_').Replace(' ', '_');
        }

        return Enum.TryParse(normalizedValue, ignoreCase, out result) && result.IsDefinedEnum();
    }

    public static bool IsValidDateRange(this DateTime? fromDate, DateTime? toDate)
    {
        return !fromDate.HasValue || !toDate.HasValue || fromDate.Value <= toDate.Value;
    }

    public static bool TryGetPaginationOffset(this int pageNumber, int pageSize, out int offset)
    {
        offset = 0;
        if (!pageNumber.IsPositive() || !pageSize.IsPositive())
        {
            return false;
        }

        var calculatedOffset = ((long)pageNumber - 1) * pageSize;
        if (calculatedOffset > int.MaxValue)
        {
            return false;
        }

        offset = (int)calculatedOffset;
        return true;
    }

    public static string HashPasswordSafe<TUser>(
        this IPasswordHasher<TUser> passwordHasher,
        TUser user,
        string password)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return passwordHasher.HashPassword(user, password);
    }

    public static PasswordVerificationResult VerifyPasswordSafe<TUser>(
        this IPasswordHasher<TUser> passwordHasher,
        TUser user,
        string hashedPassword,
        string providedPassword)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);
        return passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }

    public static string GenerateJwtToken(
        this JwtOptions options,
        int userId,
        string phoneNumber,
        string roleName,
        IEnumerable<string> permissions,
        DateTime? expiresAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        ArgumentNullException.ThrowIfNull(permissions);

        if (!userId.IsPositive())
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be positive.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.MobilePhone, phoneNumber),
            new(ClaimTypes.Role, roleName)
        };
        claims.AddRange(permissions.Distinct(StringComparer.Ordinal)
            .Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
        var expiry = expiresAtUtc
            ?? (options.TokenValidityMins is > 0
                ? DateTime.UtcNow.AddMinutes(options.TokenValidityMins.Value)
                : DateTime.UtcNow.AddDays(1));

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateRefreshToken(this int byteLength)
    {
        if (byteLength < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Refresh tokens must contain at least 16 random bytes.");
        }

        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength));
    }

    public static string HashToken(this string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
