using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Craftsman.App.Security;

public sealed class IdentitySeeder
{
    private readonly RoleManager<IdentityRole<int>> roleManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IdentitySeedOptions options;
    private readonly ILogger<IdentitySeeder> logger;

    public IdentitySeeder(
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<IdentitySeedOptions> options,
        ILogger<IdentitySeeder> logger)
    {
        this.roleManager = roleManager;
        this.userManager = userManager;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in new[] { ApplicationRoles.Admin, ApplicationRoles.Operador, ApplicationRoles.Consulta })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(role));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Nao foi possivel criar a role {role}: {FormatErrors(roleResult)}");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            logger.LogWarning("IdentitySeed AdminEmail/AdminPassword nao configurados; admin inicial nao foi criado.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(options.AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = options.AdminEmail,
                Email = options.AdminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, options.AdminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Nao foi possivel criar o admin inicial: {FormatErrors(createResult)}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, ApplicationRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Nao foi possivel adicionar Admin ao usuario inicial: {FormatErrors(addRoleResult)}");
            }
        }
    }

    private static string FormatErrors(IdentityResult result)
        => string.Join("; ", result.Errors.Select(error => error.Description));
}
