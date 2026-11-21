using System.Net;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration.Http;

public sealed class TestWebApplicationFactoryTests
{
    [Fact]
    public async Task Protected_route_should_run_through_the_http_pipeline()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/Orders/Index");

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Redirect });
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    public async Task Each_factory_should_use_an_isolated_in_memory_database()
    {
        await using var firstFactory = new TestWebApplicationFactory();
        await using var secondFactory = new TestWebApplicationFactory();
        await using var firstDb = firstFactory.CreateDbContext();
        await using var secondDb = secondFactory.CreateDbContext();

        firstDb.OrderSources.Add(new OrderSourceEntity
        {
            Id = Guid.NewGuid(),
            Name = "scenario-one",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await firstDb.SaveChangesAsync();

        Assert.NotEqual(firstFactory.DatabaseName, secondFactory.DatabaseName);
        Assert.Equal(1, await firstDb.OrderSources.CountAsync());
        Assert.Empty(await secondDb.OrderSources.ToListAsync());
    }
}
