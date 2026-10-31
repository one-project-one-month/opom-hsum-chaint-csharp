using HsumChaint.Shared.Authorization;
using Xunit;

namespace HsumChaint.Tests;

public class SeedPermissionTests
{
    [Fact]
    public void FreshDatabaseSeed_ContainsEverySystemPermissionRequiredByApi()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "HsumChaint.Database", "Scripts")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var seed = File.ReadAllText(Path.Combine(directory!.FullName, "HsumChaint.Database", "Scripts", "001_seed_test_data.sql"));
        var permissions = seed.Split("-- 2. System Permissions")[1].Split("-- Admin access")[0];
        foreach (var permission in Permissions.All)
            Assert.Contains($"('{permission}', 0)", permissions);
    }
}
