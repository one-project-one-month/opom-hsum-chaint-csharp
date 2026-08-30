using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HsumChaint.Database.Models;
using HsumChaint.Shared.Configuration;
using Scalar.AspNetCore;
using System.Text;

namespace HsumChaint.API.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }

    public static WebApplicationBuilder AddFirebaseConfiguration(this WebApplicationBuilder builder)
    {
        var configCredentialPath = builder.Configuration["Firebase:CredentialPath"];
        string configuredCredentialPath = string.IsNullOrWhiteSpace(configCredentialPath)
            ? Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH") ?? string.Empty
            : configCredentialPath;

        if (string.IsNullOrWhiteSpace(configuredCredentialPath))
        {
            return builder;
        }

        var resolvedPath = ResolveCredentialPath(builder.Environment.ContentRootPath, configuredCredentialPath);

        if (!File.Exists(resolvedPath))
        {
            return builder;
        }

        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(resolvedPath)
            });
        }

            return builder;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("JwtConfig"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSection = configuration.GetSection("JwtConfig");
                var issuer = jwtSection["Issuer"] ?? string.Empty;
                var audience = jwtSection["Audience"] ?? string.Empty;
                var secret = jwtSection["Key"] ?? string.Empty;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuerSigningKey = true
                };
            });

        return services;
    }

    private static string ResolveCredentialPath(string contentRoot, string credentialPath)
    {
        if (string.IsNullOrWhiteSpace(credentialPath))
        {
            return credentialPath;
        }

        if (Path.IsPathRooted(credentialPath))
        {
            return credentialPath;
        }

        return Path.Combine(contentRoot, credentialPath);
    }
}
