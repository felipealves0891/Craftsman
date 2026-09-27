using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Identity;

namespace Craftsman.App.E2E;

public static class E2EEndpoints
{
    public static void MapE2EEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/__e2e").AllowAnonymous();

        group.MapPost("/reset", async (
            E2EDatabaseResetter resetter,
            E2EIdentitySeeder identitySeeder,
            CancellationToken cancellationToken) =>
        {
            await resetter.ResetAsync(cancellationToken);
            var credentials = await identitySeeder.SeedAsync(cancellationToken);
            return Results.Ok(new { users = credentials });
        });

        group.MapPost("/seed-users", async (
            E2EIdentitySeeder identitySeeder,
            CancellationToken cancellationToken) =>
        {
            var credentials = await identitySeeder.SeedAsync(cancellationToken);
            return Results.Ok(new { users = credentials });
        });

        group.MapGet("/credentials", (E2EIdentitySeeder identitySeeder)
            => Results.Ok(new { users = identitySeeder.GetConfiguredUsers() }));

        group.MapPost("/login", async (
            E2ELoginRequest request,
            E2EIdentitySeeder identitySeeder,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            CancellationToken cancellationToken) =>
        {
            var credentials = identitySeeder.GetConfiguredUsers()
                .FirstOrDefault(user => string.Equals(user.Role, request.Role, StringComparison.Ordinal));

            if (credentials is null)
            {
                return Results.BadRequest(new { error = "Unknown E2E role." });
            }

            var user = await userManager.FindByEmailAsync(credentials.Email);
            if (user is null)
            {
                return Results.NotFound(new { error = "E2E user was not seeded." });
            }

            cancellationToken.ThrowIfCancellationRequested();
            await signInManager.SignInAsync(user, isPersistent: false);

            return Results.Ok(new { credentials.Role, credentials.Email });
        });
    }
}
