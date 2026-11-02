using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.Tests.Inventory;

public sealed class StockMovementTests
{
    [Theory]
    [InlineData(StockMovementType.Inbound, 4, 4)]
    [InlineData(StockMovementType.Outbound, 4, -4)]
    [InlineData(StockMovementType.Adjustment, -2, -2)]
    public void Movement_reports_signed_quantity_and_total_cost(StockMovementType type, decimal quantity, decimal signedQuantity)
    {
        var occurredAt = DateTimeOffset.Parse("2026-10-15T12:00:00+00:00");

        var movement = new StockMovement(Guid.NewGuid(), Guid.NewGuid(), type, quantity,
            "  reposição  ", "  PO-1  ", occurredAt, 2.5m);

        Assert.Equal(signedQuantity, movement.SignedQuantity);
        Assert.Equal(quantity * 2.5m, movement.TotalCostAmount);
        Assert.Equal("reposição", movement.Reason);
        Assert.Equal("PO-1", movement.BusinessReference);
        Assert.Equal(occurredAt, movement.OccurredAt);
    }

    [Fact]
    public void Movement_defaults_timestamp_and_cleans_empty_reference()
    {
        var before = DateTimeOffset.UtcNow;

        var movement = new StockMovement(Guid.NewGuid(), Guid.NewGuid(), StockMovementType.Inbound, 1,
            "entrada", "  ");

        Assert.Null(movement.BusinessReference);
        Assert.InRange(movement.OccurredAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(0, movement.UnitCostAmount);
    }

    [Fact]
    public void Movement_rejects_invalid_identifiers_quantities_reason_and_cost()
    {
        var id = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new StockMovement(Guid.Empty, materialId, StockMovementType.Inbound, 1, "entrada", null));
        Assert.Throws<ArgumentException>(() => new StockMovement(id, Guid.Empty, StockMovementType.Inbound, 1, "entrada", null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StockMovement(id, materialId, StockMovementType.Inbound, 0, "entrada", null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StockMovement(id, materialId, StockMovementType.Outbound, -1, "saída", null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StockMovement(id, materialId, StockMovementType.Adjustment, 0, "ajuste", null));
        Assert.Throws<ArgumentException>(() => new StockMovement(id, materialId, StockMovementType.Inbound, 1, " ", null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StockMovement(id, materialId, StockMovementType.Inbound, 1, "entrada", null, unitCostAmount: -1));
    }

    [Fact]
    public void Movement_throws_when_reading_signed_quantity_for_unknown_type()
    {
        var movement = new StockMovement(Guid.NewGuid(), Guid.NewGuid(), (StockMovementType)999, 1, "entrada", null);

        Assert.Throws<InvalidOperationException>(() => movement.SignedQuantity);
    }
}
