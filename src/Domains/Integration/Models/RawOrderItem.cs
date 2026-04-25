namespace Craftsman.Domain.Integration.Models;

public sealed record RawOrderItem(
    string ExternalItemId,
    string Description,
    int Quantity,
    decimal UnitPrice);
