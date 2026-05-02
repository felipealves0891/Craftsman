namespace Craftsman.Domain.Integration.Models;

public sealed record RawOrder(
    string Source,
    string ExternalOrderId,
    IReadOnlyCollection<RawOrderItem> Items);
