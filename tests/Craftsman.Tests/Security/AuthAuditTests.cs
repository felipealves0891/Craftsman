using System.Security.Claims;
using Craftsman.App.Controllers;
using Craftsman.App.Security;
using Craftsman.Domain.Events;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Craftsman.Infra.Security;
using Craftsman.Infra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Security;

public sealed class AuthAuditTests
{
    [Fact]
    public void Current_user_context_reads_authenticated_user_roles_and_request_data()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "corr-1";
        httpContext.Request.Path = "/orders";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Name, "admin@craftsman.local"),
                new Claim(ClaimTypes.Role, ApplicationRoles.Admin)
            ],
            "Test"));

        var context = new HttpCurrentUserContext(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal(42, context.Current.UserId);
        Assert.Equal("admin@craftsman.local", context.Current.UserName);
        Assert.Contains(ApplicationRoles.Admin, context.Current.RoleNames);
        Assert.Equal("corr-1", context.Current.CorrelationId);
        Assert.Equal("/orders", context.Current.RequestPath);
    }

    [Fact]
    public void Audit_redactor_masks_sensitive_fields()
    {
        var json = AuditJsonRedactor.SerializeRedacted(new
        {
            Email = "user@craftsman.local",
            PasswordHash = "hash",
            SecurityStamp = "stamp",
            RefreshToken = "refresh",
            ClientSecret = "secret"
        });

        Assert.NotNull(json);
        Assert.Contains("user@craftsman.local", json);
        Assert.DoesNotContain("hash", json);
        Assert.DoesNotContain("stamp", json);
        Assert.DoesNotContain("refresh", json);
        Assert.DoesNotContain("secret", json);
        Assert.Contains("***REDACTED***", json);
    }

    [Fact]
    public async Task Save_changes_audits_insert_update_and_delete_with_current_user()
    {
        await using var dbContext = CreateDbContext();
        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = "Produto",
            Status = "Active",
            ProductionDurationHours = 1
        };

        await dbContext.Products.AddAsync(product);
        await dbContext.SaveChangesAsync();

        product.Name = "Produto atualizado";
        await dbContext.SaveChangesAsync();

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync();

        var auditLogs = await dbContext.AuditLogs
            .Where(log => log.EntityName == nameof(ProductEntity))
            .OrderBy(log => log.Id)
            .ToListAsync();

        Assert.Collection(
            auditLogs,
            created => Assert.Equal(AuditAction.Created, created.Action),
            modified => Assert.Equal(AuditAction.Modified, modified.Action),
            deleted => Assert.Equal(AuditAction.Deleted, deleted.Action));
        Assert.All(auditLogs, log =>
        {
            Assert.Equal(7, log.UserId);
            Assert.Equal("operador@craftsman.local", log.UserName);
            Assert.Equal(ApplicationRoles.Operador, log.RoleNames);
            Assert.Equal("corr-test", log.CorrelationId);
            Assert.Equal("/test", log.RequestPath);
        });
    }

    [Fact]
    public async Task Audit_log_entity_does_not_audit_itself()
    {
        await using var dbContext = CreateDbContext();

        await dbContext.AuditLogs.AddAsync(new AuditLogEntity
        {
            OccurredAt = DateTimeOffset.UtcNow,
            Action = "Manual",
            EntityName = "Test",
            RoleNames = string.Empty
        });
        await dbContext.SaveChangesAsync();

        Assert.Equal(1, await dbContext.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task Domain_event_persistence_enriches_actor_and_correlation()
    {
        await using var dbContext = CreateDbContext();
        var handler = new DomainEventPersistenceHandler<TestDomainEvent>(
            dbContext,
            new StaticCurrentUserContext(new CurrentUserInfo(7, "operador@craftsman.local", [ApplicationRoles.Operador], "corr-test", "/test")));

        await handler.HandleAsync(new TestDomainEvent());
        await dbContext.SaveChangesAsync();

        var persisted = await dbContext.DomainEvents.SingleAsync();
        Assert.Equal(7, persisted.UserId);
        Assert.Equal("operador@craftsman.local", persisted.UserName);
        Assert.Equal("corr-test", persisted.CorrelationId);
    }

    [Fact]
    public void Controllers_declare_expected_authorization_policies()
    {
        Assert.Equal(ApplicationPolicies.Read, PolicyOn<OrdersController>());
        Assert.Equal(ApplicationPolicies.Read, PolicyOn<ProductionController>());
        Assert.Equal(ApplicationPolicies.Read, PolicyOn<ShipmentsController>());
        Assert.Equal(ApplicationPolicies.AdminOnly, PolicyOn<UsersController>());

        Assert.Equal(ApplicationPolicies.Write, PolicyOnAction<OrdersController>(nameof(OrdersController.SendToProduction)));
        Assert.Equal(ApplicationPolicies.Write, PolicyOnAction<ProductionController>(nameof(ProductionController.Advance)));
        Assert.Equal(ApplicationPolicies.Write, PolicyOnAction<ShipmentsController>(nameof(ShipmentsController.UpdateStatus)));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(
            options,
            new StaticCurrentUserContext(new CurrentUserInfo(7, "operador@craftsman.local", [ApplicationRoles.Operador], "corr-test", "/test")));
    }

    private static string? PolicyOn<TController>()
        => typeof(TController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;

    private static string? PolicyOnAction<TController>(string actionName)
        => typeof(TController).GetMethods()
            .Single(method => method.Name == actionName)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;

    private sealed record TestDomainEvent : DomainEvent
    {
    }
}
