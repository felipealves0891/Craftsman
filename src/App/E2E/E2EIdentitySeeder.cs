using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Craftsman.App.E2E;

public sealed class E2EIdentitySeeder
{
    private readonly RoleManager<IdentityRole<int>> roleManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly E2EUserOptions options;

    public E2EIdentitySeeder(
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<E2EUserOptions> options)
    {
        this.roleManager = roleManager;
        this.userManager = userManager;
        this.options = options.Value;
    }

    public async Task<IReadOnlyCollection<E2ECredentials>> SeedAsync(CancellationToken cancellationToken = default)
    {
        var users = GetConfiguredUsers();

        foreach (var role in users.Select(user => user.Role).Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<int>(role));
                ThrowIfFailed(result, $"Nao foi possivel criar a role E2E {role}");
            }
        }

        foreach (var credentials in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await userManager.FindByEmailAsync(credentials.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = credentials.Email,
                    Email = credentials.Email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, credentials.Password);
                ThrowIfFailed(createResult, $"Nao foi possivel criar o usuario E2E {credentials.Email}");
            }

            if (!await userManager.IsInRoleAsync(user, credentials.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, credentials.Role);
                ThrowIfFailed(roleResult, $"Nao foi possivel adicionar {credentials.Role} ao usuario E2E {credentials.Email}");
            }
        }

        return users;
    }

    public IReadOnlyCollection<E2ECredentials> GetConfiguredUsers()
    {
        return
        [
            new E2ECredentials(ApplicationRoles.Admin, options.AdminEmail, options.Password),
            new E2ECredentials(ApplicationRoles.Operador, options.OperadorEmail, options.Password),
            new E2ECredentials(ApplicationRoles.Consulta, options.ConsultaEmail, options.Password)
        ];
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{message}: {errors}");
    }
}
