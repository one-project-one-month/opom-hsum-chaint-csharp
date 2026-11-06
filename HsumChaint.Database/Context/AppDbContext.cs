using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HsumChaint.Database.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<SpaceRole> SpaceRoles { get; set; }

    public virtual DbSet<SpacePermission> SpacePermissions { get; set; }

    public virtual DbSet<SpaceRolePermission> SpaceRolePermissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<MonkProfile> MonkProfiles { get; set; }

    public virtual DbSet<MonasterySpace> MonasterySpaces { get; set; }

    public virtual DbSet<MonasteryMember> MonasteryMembers { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<DonorList> DonorLists { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

    public virtual DbSet<LoginMode> LoginModes { get; set; }

    public virtual DbSet<OtpCode> OtpCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Role");

            entity.HasIndex(e => e.Name, "UX_Role_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Permission");

            entity.HasIndex(e => e.Name, "UX_Permission_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Role_Permission");

            entity.HasIndex(e => new { e.RoleId, e.PermissionId }, "UX_Role_Permission_role_permission").IsUnique();
            entity.HasIndex(e => e.RoleId, "IX_Role_Permission_role_id");
            entity.HasIndex(e => e.PermissionId, "IX_Role_Permission_permission_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Role_Permission_Role_role_id");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Role_Permission_Permission_permission_id");
        });

        modelBuilder.Entity<SpaceRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Space_Role");

            entity.HasIndex(e => e.Name, "UX_Space_Role_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<SpacePermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Space_Permission");

            entity.HasIndex(e => e.Name, "UX_Space_Permission_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<SpaceRolePermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Space_Role_Permission");

            entity.HasIndex(e => new { e.SpaceRoleId, e.SpacePermissionId }, "UX_Space_Role_Permission").IsUnique();
            entity.HasIndex(e => e.SpaceRoleId, "IX_Space_Role_Permission_role_id");
            entity.HasIndex(e => e.SpacePermissionId, "IX_Space_Role_Permission_permission_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SpaceRoleId).HasColumnName("space_role_id");
            entity.Property(e => e.SpacePermissionId).HasColumnName("space_permission_id");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");

            entity.HasOne(d => d.SpaceRole).WithMany(p => p.SpaceRolePermissions)
                .HasForeignKey(d => d.SpaceRoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Space_Role_Permission_Space_Role_role_id");

            entity.HasOne(d => d.SpacePermission).WithMany(p => p.SpaceRolePermissions)
                .HasForeignKey(d => d.SpacePermissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Space_Role_Permission_Space_Permission_permission_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("User");
            entity.ToTable(t => t.HasCheckConstraint("CK_User_identifier", "phone IS NOT NULL OR email IS NOT NULL"));

            entity.HasIndex(e => new { e.PhoneNumber, e.IsDeleted }, "UX_User_phone_active").IsUnique();
            entity.HasIndex(e => new { e.Email, e.IsDeleted }, "UX_User_email_active").IsUnique();
            entity.HasIndex(e => e.RoleId, "IX_User_role_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ContactPhoneNumber)
                .HasMaxLength(50)
                .HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.PhoneVerifiedAt)
                .HasColumnType("datetime")
                .HasColumnName("phone_verified_at");
            entity.Property(e => e.EmailVerifiedAt)
                .HasColumnType("datetime")
                .HasColumnName("email_verified_at");
            entity.Property(e => e.FcmToken)
                .HasMaxLength(255)
                .HasColumnName("fcm_token");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_User_Role_role_id");
        });

        modelBuilder.Entity<LoginMode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Login_Mode");

            entity.HasIndex(e => e.Code, "UX_Login_Mode_code").IsUnique();
            entity.HasIndex(e => e.IsActive, "IX_Login_Mode_is_active");
            entity.HasIndex(e => e.UpdatedById, "IX_Login_Mode_updated_by_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .HasColumnName("code");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasColumnName("display_name");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.RequirePhoneVerification).HasColumnName("require_phone_verification");
            entity.Property(e => e.RequireEmailVerification).HasColumnName("require_email_verification");
            entity.Property(e => e.UpdatedById).HasColumnName("updated_by_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(e => e.UpdatedBy)
                .WithMany()
                .HasForeignKey(e => e.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasData(
                new LoginMode
                {
                    Id = 1,
                    Code = "PHONE_ONLY",
                    DisplayName = "Phone only",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 11, 6),
                    UpdatedAt = new DateTime(2026, 11, 6)
                },
                new LoginMode
                {
                    Id = 2,
                    Code = "EMAIL_ONLY",
                    DisplayName = "Email only",
                    IsActive = false,
                    CreatedAt = new DateTime(2026, 11, 6),
                    UpdatedAt = new DateTime(2026, 11, 6)
                },
                new LoginMode
                {
                    Id = 3,
                    Code = "PHONE_OR_EMAIL",
                    DisplayName = "Phone or email",
                    IsActive = false,
                    CreatedAt = new DateTime(2026, 11, 6),
                    UpdatedAt = new DateTime(2026, 11, 6)
                });
        });

        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Otp_Code");

            entity.HasIndex(e => e.UserId, "IX_Otp_Code_user_id");
            entity.HasIndex(e => new { e.Destination, e.Channel, e.Purpose, e.CreatedAt }, "IX_Otp_Code_lookup");
            entity.HasIndex(e => e.ExpiresAt, "IX_Otp_Code_expires_at");
            entity.HasIndex(e => new { e.UsedAt, e.ExpiresAt, e.RevokedAt }, "IX_Otp_Code_cleanup");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .HasColumnName("channel");
            entity.Property(e => e.Purpose)
                .HasMaxLength(30)
                .HasColumnName("purpose");
            entity.Property(e => e.Destination)
                .HasMaxLength(255)
                .HasColumnName("destination");
            entity.Property(e => e.CodeHash)
                .HasMaxLength(255)
                .HasColumnName("code_hash");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.IsUsed).HasColumnName("is_used");
            entity.Property(e => e.UsedAt)
                .HasColumnType("datetime")
                .HasColumnName("used_at");
            entity.Property(e => e.RevokedAt)
                .HasColumnType("datetime")
                .HasColumnName("revoked_at");
            entity.Property(e => e.FailedAttemptCount).HasColumnName("failed_attempt_count");
            entity.Property(e => e.MaxAttemptCount).HasColumnName("max_attempt_count");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MonkProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("MonkProfile");

            entity.HasIndex(e => e.UserId, "IX_MonkProfile_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.MonasteryAddress)
                .HasMaxLength(500)
                .HasColumnName("monastery_address");
            entity.Property(e => e.MonasteryName)
                .HasMaxLength(255)
                .HasColumnName("monastery_name");
        });

        modelBuilder.Entity<MonasterySpace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Monastery_Space");

            entity.HasIndex(e => e.CreatedById, "IX_Monastery_Space_created_by_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasMaxLength(500).HasColumnName("address");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.Description).HasMaxLength(1000).HasColumnName("description");
            entity.Property(e => e.MonasteryName).HasMaxLength(255).HasColumnName("monastery_name");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
        });

        modelBuilder.Entity<MonasteryMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Monastery_Member");

            entity.HasIndex(e => new { e.UserId, e.MonasterySpaceId }, "UX_Monastery_Member_user_space").IsUnique();
            entity.HasIndex(e => e.MonasterySpaceId, "IX_Monastery_Member_monastery_space_id");
            entity.HasIndex(e => e.SpaceRoleId, "IX_Monastery_Member_space_role_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsOwner).HasDefaultValue(false).HasColumnName("is_owner");
            entity.Property(e => e.MonasterySpaceId).HasColumnName("monastery_space_id");
            entity.Property(e => e.SpaceRoleId).HasColumnName("space_role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.SpaceRole).WithMany(p => p.MonasteryMembers)
                .HasForeignKey(d => d.SpaceRoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Monastery_Member_Space_Role_space_role_id");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Monastery_Member_User_user_id");

            entity.HasOne(d => d.MonasterySpace).WithMany()
                .HasForeignKey(d => d.MonasterySpaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Monastery_Member_Monastery_Space_monastery_space_id");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Invitation");

            entity.HasIndex(e => e.MonasterySpaceId, "IX_Invitation_monastery_space_id");
            entity.HasIndex(e => e.InvitedUserId, "IX_Invitation_invited_user_id");
            entity.HasIndex(e => e.InvitedById, "IX_Invitation_invited_by_id");
            entity.HasIndex(e => e.SpaceRoleId, "IX_Invitation_space_role_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.InvitedById).HasColumnName("invited_by_id");
            entity.Property(e => e.InvitedUserId).HasColumnName("invited_user_id");
            entity.Property(e => e.MonasterySpaceId).HasColumnName("monastery_space_id");
            entity.Property(e => e.SpaceRoleId).HasColumnName("space_role_id");
            entity.HasOne(e => e.SpaceRole).WithMany(p => p.Invitations)
                .HasForeignKey(e => e.SpaceRoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Invitation_Space_Role_space_role_id");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<int>();
        });

        modelBuilder.Entity<DonorList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Donor_List");

            entity.HasIndex(e => e.MonasterySpaceId, "IX_Donor_List_monastery_space_id");
            entity.HasIndex(e => e.DonorId, "IX_Donor_List_donor_id");
            entity.HasIndex(e => e.ReviewerId, "IX_Donor_List_reviewer_id");
            entity.HasIndex(e => new { e.StatusValue, e.CreatedAt }, "IX_Donor_List_status_created_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonationType).HasMaxLength(100).HasColumnName("donation_type");
            entity.Property(e => e.DonationTypeValue).HasColumnName("donation_type_value").HasConversion<int>();
            entity.Property(e => e.CustomDonationType).HasMaxLength(255).HasColumnName("custom_donation_type");
            entity.Property(e => e.DonorId).HasColumnName("donor_id");
            entity.Property(e => e.DonorName).HasMaxLength(255).HasColumnName("donor_name");
            entity.Property(e => e.Note).HasMaxLength(1000).HasColumnName("note");
            entity.Property(e => e.MonasterySpaceId).HasColumnName("monastery_space_id");
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
            entity.Property(e => e.Status).HasMaxLength(100).HasColumnName("status");
            entity.Property(e => e.StatusValue).HasColumnName("status_value").HasConversion<int>();
            entity.Property(e => e.Amount).HasPrecision(18, 2).HasColumnName("amount");
            entity.Property(e => e.Quantity).HasPrecision(18, 2).HasColumnName("quantity");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime").HasColumnName("reviewed_at");
            entity.Property(e => e.PickupTime).HasColumnType("datetime").HasColumnName("pickup_time");
            entity.Property(e => e.DropoffTime).HasColumnType("datetime").HasColumnName("dropoff_time");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime").HasColumnName("completed_at");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Notification");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_Notification_user_id_created_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message)
                .HasMaxLength(1000)
                .HasColumnName("message");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<int>();
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Refresh_Token");

            entity.HasIndex(e => e.UserId, "UX_Refresh_Token_user_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.RefreshToken1)
                .HasMaxLength(512)
                .HasColumnName("refresh_token");
            entity.Property(e => e.RevokedAt)
                .HasColumnType("datetime")
                .HasColumnName("revoked_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("User_Setting");

            entity.HasIndex(e => e.UserId, "UX_User_Setting_user_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DropoffNotificationTime)
                .HasColumnType("datetime")
                .HasColumnName("dropoff_notification_time");
            entity.Property(e => e.DropoffTime)
                .HasColumnType("datetime")
                .HasColumnName("dropoff_time");
            entity.Property(e => e.PickupNotificationTime)
                .HasColumnType("datetime")
                .HasColumnName("pickup_notification_time");
            entity.Property(e => e.PickupTime)
                .HasColumnType("datetime")
                .HasColumnName("pickup_time");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
