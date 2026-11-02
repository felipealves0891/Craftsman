using Craftsman.App.Security;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Craftsman.Tests.Security;

public sealed class IdentitySeederTests
{
    [Fact]
    public async Task Identity_seeder_creates_roles_and_configured_admin_once()
    {
        await using var dbContext = CreateDbContext();
        using var provider = CreateIdentityProvider(dbContext);
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var withoutAdmin = new IdentitySeeder(
            roleManager,
            userManager,
            Options.Create(new IdentitySeedOptions()),
            NullLogger<IdentitySeeder>.Instance);
        await withoutAdmin.SeedAsync();

        Assert.Equal(3, await dbContext.Roles.CountAsync());
        Assert.Empty(await dbContext.Users.ToListAsync());

        var withAdmin = new IdentitySeeder(
            roleManager,
            userManager,
            Options.Create(new IdentitySeedOptions
            {
                AdminEmail = "admin@example.com",
                AdminPassword = "Admin123!"
            }),
            NullLogger<IdentitySeeder>.Instance);
        await withAdmin.SeedAsync();
        await withAdmin.SeedAsync();

        var admin = await userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(admin);
        Assert.Equal([ApplicationRoles.Admin], await userManager.GetRolesAsync(admin));
        Assert.Single(await dbContext.Users.ToListAsync());
    }

    private static ServiceProvider CreateIdentityProvider(AppDbContext dbContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dbContext);
        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services.BuildServiceProvider();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
