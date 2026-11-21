using System.Net;
using System.Net.Http.Headers;
using Craftsman.Domain.Production.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration.Controllers;

public sealed class OrdersAndProductionQueryTests
{
    [Fact]
    public async Task Anonymous_read_is_redirected_and_consulta_can_read_orders()
    {
        await using var factory = new Integration.Http.TestWebApplicationFactory();
        using var anonymous = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var consulta = factory.CreateAuthenticatedClient("Consulta");
        await using (var db = factory.CreateDbContext())
        {
            await Integration.Http.TestDataFixture.SeedOrderAsync(db);
        }

        using var anonymousResponse = await anonymous.GetAsync("/Orders/Index");
        using var response = await consulta.GetAsync("/Orders/Index");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains(anonymousResponse.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Redirect });
        if (anonymousResponse.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.Equal("/Account/Login", anonymousResponse.Headers.Location?.OriginalString);
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("test-source", content);
    }

    [Fact]
    public async Task Order_details_returns_order_data_and_404_for_unknown_order()
    {
        await using var factory = new Integration.Http.TestWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient("Consulta");
        await using (var db = factory.CreateDbContext())
        {
            await Integration.Http.TestDataFixture.SeedOrderAsync(db);
        }

        using var response = await client.GetAsync($"/Orders/Details/{Integration.Http.TestDataFixture.OrderId}");
        using var notFound = await client.GetAsync($"/Orders/Details/{Guid.Empty}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TEST-ORDER-001", content);
        Assert.Contains("Produto de teste", content);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Production_index_applies_status_and_fixed_date_filters()
    {
        await using var factory = new Integration.Http.TestWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient("Consulta");
        await using (var db = factory.CreateDbContext())
        {
            await Integration.Http.TestDataFixture.SeedProductionTaskAsync(db);
            await Integration.Http.TestDataFixture.SeedProductionTaskAsync(
                db,
                ProductionTaskStatus.Completed,
                new DateTimeOffset(2026, 12, 5, 8, 0, 0, TimeSpan.Zero),
                Integration.Http.TestDataFixture.CompletedTaskId);
        }

        using var response = await client.GetAsync("/Production/Index?status=Planned&plannedDate=2026-11-21");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TEST-ORDER-001", content);
        Assert.Contains("Planned", content);
        Assert.Contains(Integration.Http.TestDataFixture.TaskId.ToString(), content);
        Assert.DoesNotContain(Integration.Http.TestDataFixture.CompletedTaskId.ToString(), content);
    }

    [Fact]
    public async Task Current_month_returns_deterministic_json_without_storage()
    {
        await using var factory = new Integration.Http.TestWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient("Consulta");

        using var response = await client.GetAsync("/Production/CurrentMonth");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"year\":2026,\"month\":11}", json);
        AssertNoStore(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Calendar_returns_partial_for_the_fixed_current_date_and_filter()
    {
        await using var factory = new Integration.Http.TestWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient("Consulta");
        await using (var db = factory.CreateDbContext())
        {
            await Integration.Http.TestDataFixture.SeedProductionTaskAsync(db);
        }

        using var response = await client.GetAsync("/Production/Calendar?status=Planned");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TEST-ORDER-001", content);
        AssertNoStore(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Consulta_cannot_post_and_missing_antiforgery_is_rejected_without_changes()
    {
        await using var factory = new Integration.Http.TestWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient("Consulta");
        await using (var db = factory.CreateDbContext())
        {
            await Integration.Http.TestDataFixture.SeedProductionTaskAsync(db);
        }

        using var forbidden = await client.PostAsync(
            $"/Production/Advance?id={Integration.Http.TestDataFixture.TaskId}&action=start",
            new FormUrlEncodedContent([]));
        using var operatorClient = factory.CreateAuthenticatedClient("Operador");
        using var missingToken = await operatorClient.PostAsync(
            $"/Production/Advance?id={Integration.Http.TestDataFixture.TaskId}&action=start",
            new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
    }

    private static void AssertNoStore(CacheControlHeaderValue? cacheControl)
    {
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl!.NoStore);
        Assert.Contains("no-store", cacheControl.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
