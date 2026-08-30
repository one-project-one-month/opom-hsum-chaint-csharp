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

    public virtual DbSet<DonorList> DonorLists { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<MonasteryMember> MonasteryMembers { get; set; }

    public virtual DbSet<MonasterySpace> MonasterySpaces { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<MonkProfile> MonkProfiles { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=.;Database=HsumChaint;User Id=sa;Password=sasa@123;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DonorList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Donor_Li__3213E83FBE8D8E54");

            entity.ToTable("Donor_List");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonationType).HasColumnName("donation_type");
            entity.Property(e => e.DonationTypeValue).HasColumnName("donation_type_value");
            entity.Property(e => e.CustomDonationType).HasColumnName("custom_donation_type");
            entity.Property(e => e.DonorId).HasColumnName("donor_id");
            entity.Property(e => e.DonorName).HasColumnName("donor_name");
            entity.Property(e => e.MonasterySpaceId).HasColumnName("monastery_space_id");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 2);
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.StatusValue).HasColumnName("status_value");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ReviewedAt)
                .HasColumnType("datetime")
                .HasColumnName("reviewed_at");
            entity.Property(e => e.PickupTime)
                .HasColumnType("datetime")
                .HasColumnName("pickup_time");
            entity.Property(e => e.DropoffTime)
                .HasColumnType("datetime")
                .HasColumnName("dropoff_time");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.DonationTypeValue).HasConversion<int>();
            entity.Property(e => e.StatusValue).HasConversion<int>();
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invitati__3213E83FE0EC0065");

            entity.ToTable("Invitation");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.InvitedById).HasColumnName("invited_by_id");
            entity.Property(e => e.InvitedUserId).HasColumnName("invited_user_id");
            entity.Property(e => e.MonasterySpaceId).HasColumnName("monastery_space_id");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.Property(u => u.Role)
                  .HasConversion<int>();

            entity.Property(u => u.Status)
                  .HasConversion<int>();
        });

        modelBuilder.Entity<MonasteryMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Monaster__3213E83F67FE23DF");

            entity.ToTable("Monastery_Member");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsOwner).HasColumnName("isOwner");
            entity.Property(e => e.MonasterySpaceId).HasColumnName("monastery_space_id");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role).HasConversion<int>();
        });

        modelBuilder.Entity<MonasterySpace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Monaster__3213E83F36DD49BB");

            entity.ToTable("Monastery_Space");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MonasteryName).HasColumnName("monastery_name");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3213E83FC7D966AA");

            entity.ToTable("Notification");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("isRead");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsDelete).HasColumnName("isDelete");
        });

        modelBuilder.Entity<Notification>()
            .Property(u => u.Type)
            .HasConversion<int>(); // maps enum to int automatically

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Refresh___3213E83FE847A65A");

            entity.ToTable("Refresh_Token");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.RefreshToken1).HasColumnName("refresh_token");
            entity.Property(e => e.RevokedAt)
                .HasColumnType("datetime")
                .HasColumnName("revoked_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("User");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContactPhoneNumber)
                .HasMaxLength(50)
                .HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");;
            entity.Property(e => e.FcmToken)
                .HasMaxLength(255)
                .HasColumnName("fcm_token");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name"); ;
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
            entity.Property(e => e.UserType)
                .HasColumnName("user_type");
        });

        modelBuilder.Entity<User>()
            .Property(u => u.UserType)
            .HasConversion<int>(); // maps enum to int automatically

        modelBuilder.Entity<MonkProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("MonkProfile");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.MonasteryAddress)
                .HasMaxLength(500)
                .HasColumnName("monastery_address");
            entity.Property(e => e.MonasteryName)
                .HasMaxLength(255)
                .HasColumnName("monastery_name");
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User_Set__3213E83F73A6F051");

            entity.ToTable("User_Setting");

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
