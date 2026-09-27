using Craftsman.App.Services;
using Craftsman.App.E2E;
using Craftsman.App.Security;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Security;
using Craftsman.Infra.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents();
builder.Services.AddCraftsmanApplication();
builder.Services.AddCraftsmanInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

builder.Services.Configure<IdentitySeedOptions>(builder.Configuration.GetSection(IdentitySeedOptions.SectionName));
builder.Services.Configure<E2EUserOptions>(builder.Configuration.GetSection(E2EUserOptions.SectionName));
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(ApplicationPolicies.Read, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.Operador, ApplicationRoles.Consulta));
    options.AddPolicy(ApplicationPolicies.Write, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.Operador));
    options.AddPolicy(ApplicationPolicies.AdminOnly, policy => policy.RequireRole(ApplicationRoles.Admin));
});

builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<E2EDatabaseResetter>();
builder.Services.AddScoped<E2EIdentitySeeder>();
builder.Services.AddScoped<E2EOrderScenarioSeeder>();
builder.Services.AddScoped<E2EManualOrderScenarioSeeder>();
builder.Services.AddScoped<E2EProductCatalogScenarioSeeder>();

var app = builder.Build();

var supportedCulture = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(supportedCulture),
    SupportedCultures = [supportedCulture],
    SupportedUICultures = [supportedCulture]
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("E2E"))
{
    Console.WriteLine($"Running in {app.Environment.EnvironmentName} environment. Applying migrations and seeding data...");

    app.UseDeveloperExceptionPage();
    using var scope = app.Services.CreateScope();

    await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await identitySeeder.SeedAsync();
    }
    else
    {
        var e2eIdentitySeeder = scope.ServiceProvider.GetRequiredService<E2EIdentitySeeder>();
        await e2eIdentitySeeder.SeedAsync();
    }
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken) ? Results.Ok() : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

if (app.Environment.IsEnvironment("E2E"))
{
    app.MapE2EEndpoints();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
