using System.Linq;
using HsumChaint.Database.Models;
using HsumChaint.Shared.Authorization;

namespace HsumChaint.Tests;

internal static class RbacTestData
{
    public static void Seed(AppDbContext db)
    {
        db.Roles.AddRange(
            new Role { Id = SystemRoles.AdminId, Name = SystemRoles.Admin },
            new Role { Id = SystemRoles.MonkId, Name = SystemRoles.Monk },
            new Role { Id = SystemRoles.DonorId, Name = SystemRoles.Donor },
            new Role { Id = 4, Name = "Editor" },
            new Role { Id = 5, Name = "Viewer" });

        var id = 1;
        foreach (var name in Permissions.All)
        {
            db.Permissions.Add(new Permission { Id = id, Name = name });
            db.RolePermissions.Add(new RolePermission { RoleId = 1, PermissionId = id });
            if (name.StartsWith("Monastery.") || name.StartsWith("Donation.") || name.StartsWith("Invitation.") || name.StartsWith("Notification.") || name == Permissions.Users.Update)
                db.RolePermissions.Add(new RolePermission { RoleId = 2, PermissionId = id });
            if (name is Permissions.Monastery.View or Permissions.Donation.View or Permissions.Donation.Create or Permissions.Donation.Cancel
                or Permissions.Invitation.View or Permissions.Invitation.Respond or Permissions.Notification.View or Permissions.Notification.Manage or Permissions.Users.Update)
                db.RolePermissions.Add(new RolePermission { RoleId = 3, PermissionId = id });
            if (name is Permissions.Donation.Schedule or Permissions.Donation.View or Permissions.Monastery.View)
                db.RolePermissions.Add(new RolePermission { RoleId = 4, PermissionId = id });
            id++;
        }

        var spaceOwner = new SpaceRole { Id = SpaceRoles.OwnerId, Name = SpaceRoles.Owner };
        var spaceAdmin = new SpaceRole { Id = SpaceRoles.AdminId, Name = SpaceRoles.Admin };
        var spaceEditor = new SpaceRole { Id = SpaceRoles.EditorId, Name = SpaceRoles.Editor };
        var spaceViewer = new SpaceRole { Id = SpaceRoles.ViewerId, Name = SpaceRoles.Viewer };

        db.SpaceRoles.AddRange(spaceOwner, spaceAdmin, spaceEditor, spaceViewer);

        var spacePermId = 1;
        var srpId = 1;
        foreach (var name in SpacePermissions.All)
        {
            var sp = new SpacePermission { Id = spacePermId, Name = name };
            db.SpacePermissions.Add(sp);

            // Owner (1)
            db.SpaceRolePermissions.Add(new SpaceRolePermission
            {
                Id = srpId++,
                SpaceRoleId = SpaceRoles.OwnerId,
                SpacePermissionId = spacePermId,
                SpaceRole = spaceOwner,
                SpacePermission = sp
            });

            // Admin (2)
            if (name is SpacePermissions.SpaceView or SpacePermissions.SpaceUpdate
                or SpacePermissions.SpaceInviteMember or SpacePermissions.SpaceUpdateMemberRole or SpacePermissions.SpaceRemoveMember
                or SpacePermissions.SpaceViewMembers
                or SpacePermissions.DonationView or SpacePermissions.DonationCreate or SpacePermissions.DonationReview
                or SpacePermissions.DonationSchedule or SpacePermissions.DonationCancel or SpacePermissions.DonationDelete)
            {
                db.SpaceRolePermissions.Add(new SpaceRolePermission
                {
                    Id = srpId++,
                    SpaceRoleId = SpaceRoles.AdminId,
                    SpacePermissionId = spacePermId,
                    SpaceRole = spaceAdmin,
                    SpacePermission = sp
                });
            }

            // Editor (3)
            if (name is SpacePermissions.SpaceView or SpacePermissions.DonationView or SpacePermissions.DonationCreate
                or SpacePermissions.DonationReview or SpacePermissions.DonationSchedule or SpacePermissions.DonationCancel or SpacePermissions.DonationDelete)
            {
                db.SpaceRolePermissions.Add(new SpaceRolePermission
                {
                    Id = srpId++,
                    SpaceRoleId = SpaceRoles.EditorId,
                    SpacePermissionId = spacePermId,
                    SpaceRole = spaceEditor,
                    SpacePermission = sp
                });
            }

            // Viewer (4)
            if (name is SpacePermissions.SpaceView or SpacePermissions.DonationView or SpacePermissions.DonationCreate)
            {
                db.SpaceRolePermissions.Add(new SpaceRolePermission
                {
                    Id = srpId++,
                    SpaceRoleId = SpaceRoles.ViewerId,
                    SpacePermissionId = spacePermId,
                    SpaceRole = spaceViewer,
                    SpacePermission = sp
                });
            }

            spacePermId++;
        }

        db.SaveChanges();
    }
}
