using Microsoft.AspNetCore.Authorization;

namespace HsumChaint.API.Authorization;

public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission);
