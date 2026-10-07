using HsumChaint.Shared;
using Microsoft.AspNetCore.Mvc;

namespace HsumChaint.API.Extensions;

public static class ActionResultExtensions
{
    public static IActionResult ToActionResult(this Result response)
    {
        if (response.IsSuccess) return new OkObjectResult(response);

        if (response.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return new NotFoundObjectResult(response);

        if (response.Message.Contains("not authorized", StringComparison.OrdinalIgnoreCase)
            || response.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
            || response.Message.Contains("not a member", StringComparison.OrdinalIgnoreCase))
            return new ObjectResult(response) { StatusCode = StatusCodes.Status403Forbidden };

        return new BadRequestObjectResult(response);
    }
}
