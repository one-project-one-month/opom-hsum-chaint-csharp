using HsumChaint.Domain.Features.RolePermission.ServiceInterfaces;
using HsumChaint.Domain.Features.RolePermission.Services;
using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Domain.Features.Auth.Services;
using HsumChaint.Domain.Features.Donation.ServiceInterfaces;
using HsumChaint.Domain.Features.Donation.Services;
using HsumChaint.Domain.Features.Monastery.ServiceInterfaces;
using HsumChaint.Domain.Features.Monastery.Services;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Domain.Features.Notification.ServiceInterfaces;
using HsumChaint.Domain.Features.Notification.Services;
using HsumChaint.Domain.Features.User.ServiceInterfaces;
using HsumChaint.Domain.Features.User.Services;
using HsumChaint.Database.Models;
using HsumChaint.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsumChaint.Domain.Features;

public static class FeatureManager
{
    public static WebApplicationBuilder AddDomain(this WebApplicationBuilder builder)
    {
        var databaseOptions = builder.Configuration
            .GetSection("ConnectionStrings")
            .Get<DatabaseOptions>();
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? databaseOptions?.DefaultConnection
            ?? string.Empty;

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IMonasteryService, MonasteryService>();
        builder.Services.AddScoped<IDonationService, DonationService>();
        builder.Services.AddScoped<IFirebaseNotificationProvider, FirebaseNotificationProvider>();

        return builder;
    }
}
