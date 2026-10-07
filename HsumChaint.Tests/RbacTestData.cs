using HsumChaint.Database.Models;
using HsumChaint.Shared.Authorization;

namespace HsumChaint.Tests;

internal static class RbacTestData
{
    public static void Seed(AppDbContext db)
    {
        db.Roles.AddRange(new Role { Id = 1, Name = "Admin" }, new Role { Id = 2, Name = "Monk" },
            new Role { Id = 3, Name = "User" }, new Role { Id = 4, Name = "Editor" }, new Role { Id = 5, Name = "Viewer" });
        var id = 1;
        foreach (var name in Permissions.All)
        {
            db.Permissions.Add(new Permission { Id = id, Name = name });
            db.RolePermissions.Add(new RolePermission { RoleId = 1, PermissionId = id });
            if (name.StartsWith("Monastery.") || name.StartsWith("Donation."))
                db.RolePermissions.Add(new RolePermission { RoleId = 2, PermissionId = id });
            if (name is Permissions.Donation.Schedule or Permissions.Donation.View or Permissions.Monastery.View)
                db.RolePermissions.Add(new RolePermission { RoleId = 4, PermissionId = id });
            id++;
        }
        db.SaveChanges();
    }
}
