using System.Net;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Craftsman.Infra.Security;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration.Http;

public sealed class OrdersProductionMutationTests
{
    [Fact]
    public async Task Send_to_production_for_eligible_order_creates_task_consumes_stock_and_audits()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionPlanningScenarioAsync(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Orders/Index");
        using var response = await client.PostAsync(
            $"/Orders/SendToProduction?id={TestDataFixture.OrderId}",
            HttpTestHelpers.AntiforgeryForm(token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith($"/Orders/Details/{TestDataFixture.OrderId}", response.Headers.Location?.OriginalString);

        await using var verification = factory.CreateDbContext();
        var task = await verification.ProductionTasks.SingleAsync();
        var order = await verification.Orders.SingleAsync();
        var consumption = await verification.StockMovements
            .Where(movement => movement.Type == StockMovementType.Outbound.ToString())
            .SingleAsync();
        var audit = await verification.AuditLogs
            .Where(log => log.Action == AuditAction.SendToProduction)
            .SingleAsync();

        Assert.Equal(OrderStatus.ReadyForProduction.ToString(), order.Status);
        Assert.Equal(ProductStatus.Active.ToString(), (await verification.Products.SingleAsync()).Status);
        Assert.Equal(ProductionTaskStatus.Planned.ToString(), task.Status);
        Assert.Equal(TestDataFixture.OrderId, task.OrderId);
        Assert.Equal(TestDataFixture.RawMaterialId, consumption.RawMaterialId);
        Assert.Equal(4m, consumption.Quantity);
        Assert.Equal(task.Id.ToString(), consumption.BusinessReference);
        Assert.Equal(AuditAction.SendToProduction, audit.Action);
        Assert.Equal(nameof(OrderEntity).Replace("Entity", string.Empty), audit.EntityName);
        Assert.Equal(TestDataFixture.OrderId.ToString(), audit.EntityId);
        Assert.Contains("Planned", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("true", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Send_to_production_with_insufficient_stock_has_no_partial_business_effects()
    {
        await AssertPlanningRejectionHasNoBusinessEffectsAsync(
            db => TestDataFixture.SeedProductionPlanningScenarioAsync(db, stockQuantity: 3m));
    }

    [Fact]
    public async Task Send_to_production_with_inactive_product_has_no_partial_business_effects()
    {
        await AssertPlanningRejectionHasNoBusinessEffectsAsync(
            db => TestDataFixture.SeedProductionPlanningScenarioAsync(db, productStatus: ProductStatus.Inactive));
    }

    [Fact]
    public async Task Send_to_production_with_invalid_bill_of_materials_has_no_partial_business_effects()
    {
        await AssertPlanningRejectionHasNoBusinessEffectsAsync(
            db => TestDataFixture.SeedProductionPlanningScenarioAsync(db, includeBillOfMaterials: false));
    }

    [Fact]
    public async Task Advance_with_missing_task_redirects_without_business_effects_or_audit()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Production/Index");

        using var response = await client.PostAsync(
            $"/Production/Advance?id={Guid.NewGuid()}",
            HttpTestHelpers.AntiforgeryForm(token, ("action", "start")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var verification = factory.CreateDbContext();
        Assert.Empty(await verification.ProductionTasks.ToListAsync());
        Assert.Empty(await verification.Orders.ToListAsync());
        Assert.DoesNotContain(
            await verification.AuditLogs.ToListAsync(),
            log => log.Action == AuditAction.ProductionAdvanced);
    }

    [Fact]
    public async Task Advance_with_invalid_transition_preserves_state_and_does_not_audit()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionTaskAsync(db, ProductionTaskStatus.InProduction);
            var order = await db.Orders.SingleAsync();
            order.Status = OrderStatus.InProduction.ToString();
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Production/Index");
        using var response = await client.PostAsync(
            $"/Production/Advance?id={TestDataFixture.TaskId}",
            HttpTestHelpers.AntiforgeryForm(token, ("action", "start")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var verification = factory.CreateDbContext();
        Assert.Equal(ProductionTaskStatus.InProduction.ToString(), (await verification.ProductionTasks.SingleAsync()).Status);
        Assert.Equal(OrderStatus.InProduction.ToString(), (await verification.Orders.SingleAsync()).Status);
        Assert.DoesNotContain(
            await verification.AuditLogs.ToListAsync(),
            log => log.Action == AuditAction.ProductionAdvanced);
    }

    [Fact]
    public async Task Advance_success_records_production_audit()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionTaskAsync(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Production/Index");
        using var response = await client.PostAsync(
            $"/Production/Advance?id={TestDataFixture.TaskId}",
            HttpTestHelpers.AntiforgeryForm(token, ("action", "start")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var verification = factory.CreateDbContext();
        var auditLogs = await verification.AuditLogs.ToListAsync();
        Assert.True(
            auditLogs.Any(log => log.Action == AuditAction.ProductionAdvanced),
            string.Join(" | ", auditLogs.Select(log => $"{log.Action}:{log.EntityName}:{log.EntityId}:{log.AfterJson}")));
        var audit = auditLogs.Single(log => log.Action == AuditAction.ProductionAdvanced);
        Assert.Equal("ProductionTask", audit.EntityName);
        Assert.Equal(TestDataFixture.TaskId.ToString(), audit.EntityId);
        Assert.Contains("start", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertPlanningRejectionHasNoBusinessEffectsAsync(
        Func<AppDbContext, Task> seed)
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await seed(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Orders/Index");
        using var response = await client.PostAsync(
            $"/Orders/SendToProduction?id={TestDataFixture.OrderId}",
            HttpTestHelpers.AntiforgeryForm(token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var verification = factory.CreateDbContext();
        Assert.Empty(await verification.ProductionTasks.ToListAsync());
        Assert.Equal(OrderStatus.Normalized.ToString(), (await verification.Orders.SingleAsync()).Status);
        Assert.Empty(await verification.StockMovements
            .Where(movement => movement.Type == StockMovementType.Outbound.ToString())
            .ToListAsync());
        var audit = await verification.AuditLogs.SingleAsync(log => log.Action == AuditAction.SendToProduction);
        Assert.Contains("Planned", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("false", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Consulta_role_cannot_advance_production()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient("Consulta");

        using var response = await client.PostAsync(
            $"/Production/Advance?id={TestDataFixture.TaskId}",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("action", "start")]));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Authorized_mutation_without_antiforgery_is_rejected_without_effects()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionTaskAsync(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        using var response = await client.PostAsync(
            $"/Production/Advance?id={TestDataFixture.TaskId}",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("action", "start")]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var verification = factory.CreateDbContext();
        Assert.Equal(ProductionTaskStatus.Planned.ToString(), (await verification.ProductionTasks.SingleAsync()).Status);
    }

    [Fact]
    public async Task Send_to_production_with_missing_product_redirects_without_creating_task()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedOrderAsync(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Orders/Index");

        using var response = await client.PostAsync(
            $"/Orders/SendToProduction?id={TestDataFixture.OrderId}",
            HttpTestHelpers.AntiforgeryForm(token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith($"/Orders/Details/{TestDataFixture.OrderId}", response.Headers.Location?.OriginalString);

        await using var verification = factory.CreateDbContext();
        Assert.Empty(await verification.ProductionTasks.ToListAsync());
        Assert.Equal(OrderStatus.Normalized.ToString(), (await verification.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Advance_start_updates_task_and_order_through_http_pipeline()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionTaskAsync(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Production/Index");

        using var response = await client.PostAsync(
            $"/Production/Advance?id={TestDataFixture.TaskId}",
            HttpTestHelpers.AntiforgeryForm(token, ("action", "start")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/Production", response.Headers.Location?.OriginalString);

        await using var verification = factory.CreateDbContext();
        Assert.Equal(ProductionTaskStatus.InProduction.ToString(), (await verification.ProductionTasks.SingleAsync()).Status);
        Assert.Equal(OrderStatus.InProduction.ToString(), (await verification.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Revert_production_removes_tasks_and_normalizes_order()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionTaskAsync(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Orders/Index");
        using var response = await client.PostAsync(
            $"/Orders/RevertProduction?id={TestDataFixture.OrderId}",
            HttpTestHelpers.AntiforgeryForm(token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var secondResponse = await client.PostAsync(
            $"/Orders/RevertProduction?id={TestDataFixture.OrderId}",
            HttpTestHelpers.AntiforgeryForm(token));
        Assert.Equal(HttpStatusCode.Redirect, secondResponse.StatusCode);

        await using var verification = factory.CreateDbContext();
        Assert.Empty(await verification.ProductionTasks.ToListAsync());
        Assert.Equal(OrderStatus.Normalized.ToString(), (await verification.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Delete_removes_order_and_dependencies_through_http_pipeline()
    {
        await using var factory = new TestWebApplicationFactory();
        await using (var db = factory.CreateDbContext())
        {
            await TestDataFixture.SeedOrderAsync(db);
        }

        using var client = factory.CreateAuthenticatedClient("Operador");
        var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Orders/Index");
        using var response = await client.PostAsync(
            $"/Orders/Delete?id={TestDataFixture.OrderId}",
            HttpTestHelpers.AntiforgeryForm(token));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/Orders", response.Headers.Location?.OriginalString);
        await using var verification = factory.CreateDbContext();
        Assert.Empty(await verification.Orders.ToListAsync());
        Assert.Single(await verification.OrderDeletionProcesses.ToListAsync());
    }

    [Fact]
    public async Task Advance_complete_and_cancel_apply_their_allowed_transitions()
    {
        await using var completeFactory = new TestWebApplicationFactory();
        await using (var db = completeFactory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionTaskAsync(db, ProductionTaskStatus.InProduction);
            var order = await db.Orders.SingleAsync();
            order.Status = OrderStatus.InProduction.ToString();
            await db.SaveChangesAsync();
        }

        using (var client = completeFactory.CreateAuthenticatedClient("Operador"))
        {
            var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Production/Index");
            using var response = await client.PostAsync(
                $"/Production/Advance?id={TestDataFixture.TaskId}",
                HttpTestHelpers.AntiforgeryForm(token, ("action", "complete")));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        await using var completeVerification = completeFactory.CreateDbContext();
        Assert.Equal(ProductionTaskStatus.Completed.ToString(), (await completeVerification.ProductionTasks.SingleAsync()).Status);

        await using var cancelFactory = new TestWebApplicationFactory();
        await using (var db = cancelFactory.CreateDbContext())
        {
            await TestDataFixture.SeedProductionTaskAsync(db);
        }

        using (var client = cancelFactory.CreateAuthenticatedClient("Operador"))
        {
            var token = await HttpTestHelpers.GetAntiforgeryTokenAsync(client, "/Production/Index");
            using var response = await client.PostAsync(
                $"/Production/Advance?id={TestDataFixture.TaskId}",
                HttpTestHelpers.AntiforgeryForm(token, ("action", "cancel")));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        await using var cancelVerification = cancelFactory.CreateDbContext();
        Assert.Equal(ProductionTaskStatus.Cancelled.ToString(), (await cancelVerification.ProductionTasks.SingleAsync()).Status);
    }
}
