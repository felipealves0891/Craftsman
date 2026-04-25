using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Shipping;

public sealed class ShipmentRepositoryTests
{
    [Fact]
    public async Task Shipment_repository_saves_and_lists_by_status()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ShipmentRepository(dbContext);
        var shipment = new Shipment(Guid.NewGuid(), Guid.NewGuid(), "TRACK-1");

        await repository.AddAsync(shipment);
        await dbContext.SaveChangesAsync();

        var shipments = await repository.ListAsync(ShipmentStatus.Created);

        var loaded = Assert.Single(shipments);
        Assert.Equal(shipment.Id, loaded.Id);
        Assert.Empty(loaded.DomainEvents);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
