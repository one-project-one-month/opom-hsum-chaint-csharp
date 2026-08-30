using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HsumChaint.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetCurrentUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(value, out var userId) ? userId : null;
    }
}
