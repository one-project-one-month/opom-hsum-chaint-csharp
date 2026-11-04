namespace HsumChaint.Shared.Authorization;

/// <summary>
/// Overall system user roles. Every user in the system has one of these roles.
/// System roles are:
/// - Admin: Full system administrator
/// - Monk: Monastic user with monastery creation permissions
/// - Donor / User: General member and donor
/// </summary>
public static class SystemRoles
{
    public const int AdminId = 1;
    public const int MonkId = 2;
    public const int DonorId = 3;
    public const int UserId = 3; // Backward-compatible alias for existing DB role name

    public const string Admin = "Admin";
    public const string Monk = "Monk";
    public const string Donor = "User"; // Maps to database role name 'User'
    public const string User = "User";
}
