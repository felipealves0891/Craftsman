using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Craftsman.Infra.Persistence;

public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var appSettingsPath = ResolveAppSettingsPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(appSettingsPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("CraftsmanDb")
            ?? throw new InvalidOperationException("Connection string 'CraftsmanDb' was not configured.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveAppSettingsPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directAppPath = Path.Combine(currentDirectory, "src", "App");
        if (Directory.Exists(directAppPath))
        {
            return directAppPath;
        }

        var siblingAppPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "App"));
        if (Directory.Exists(siblingAppPath))
        {
            return siblingAppPath;
        }

        var currentAppPath = Path.GetFullPath(currentDirectory);
        if (File.Exists(Path.Combine(currentAppPath, "appsettings.json")))
        {
            return currentAppPath;
        }

        throw new InvalidOperationException("Could not locate the App project appsettings files.");
    }
}
