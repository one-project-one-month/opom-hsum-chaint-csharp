using HsumChaint.Shared.Configuration;
using HsumChaint.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace HsumChaint.Database.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection("ConnectionStrings").Get<DatabaseOptions>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? databaseOptions?.DefaultConnection
            ?? string.Empty;

        services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString
            , ServerVersion.AutoDetect(connectionString)));

        return services;
    }
}
