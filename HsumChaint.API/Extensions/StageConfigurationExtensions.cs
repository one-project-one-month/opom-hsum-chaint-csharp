using Microsoft.Extensions.Configuration;
using System.IO;

namespace HsumChaint.API.Extensions;

public static class StageConfigurationExtensions
{
    public static WebApplicationBuilder AddStageConfig(this WebApplicationBuilder builder)
    {
        var stage = (builder.Configuration["Stage"] ?? builder.Environment.EnvironmentName ?? "development").ToLowerInvariant();

        var configDirectory = GetConfigDirectory(builder.Environment.ContentRootPath);
        if (configDirectory is null)
        {
            builder.Configuration["Stage"] = stage;
            return builder;
        }

        var appSettingsPath = Path.Combine(configDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: true);
        }

        var customSettingsPath = Path.Combine(configDirectory, $"custom-settings-{stage}.json");
        if (File.Exists(customSettingsPath))
        {
            builder.Configuration.AddJsonFile(customSettingsPath, optional: true, reloadOnChange: true);
        }

        builder.Configuration["Stage"] = stage;
        return builder;
    }

    private static string? GetConfigDirectory(string startPath)
    {
        var currentPath = startPath;

        while (!string.IsNullOrWhiteSpace(currentPath))
        {
            var configPath = Path.Combine(currentPath, "Config");
            if (Directory.Exists(configPath))
            {
                return configPath;
            }

            currentPath = Path.GetDirectoryName(currentPath);
        }

        return null;
    }
}
