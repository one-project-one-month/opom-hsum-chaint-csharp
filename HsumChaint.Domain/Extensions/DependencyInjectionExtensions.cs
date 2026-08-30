using HsumChaint.Domain.Features.Auth.ServiceInterfaces;
using HsumChaint.Domain.Features.Auth.Services;
using HsumChaint.Domain.Features.Notification.Providers;
using HsumChaint.Domain.Features.Notification.ServiceInterfaces;
using HsumChaint.Domain.Features.Notification.Services;
using HsumChaint.Domain.Features.User.ServiceInterfaces;
using HsumChaint.Domain.Features.User.Services;
using HsumChaint.Domain.Mappings;
using Microsoft.Extensions.DependencyInjection;

namespace HsumChaint.Domain.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddAutoMapper(config =>
        {
            config.AddMaps(typeof(DtoToEntityMappingProfile).Assembly);
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, NotificationServices>();
        services.AddScoped<IFirebaseNotificationProvider, FirebaseNotificationProvider>();

        return services;
    }
}
