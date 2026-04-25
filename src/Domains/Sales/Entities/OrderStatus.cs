namespace Craftsman.Domain.Sales.Entities;

public enum OrderStatus
{
    Normalized = 1,
    ReadyForProduction = 2,
    InProduction = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}
