using System.Diagnostics.CodeAnalysis;
namespace Craftsman.Domain.Integration.Models;

[ExcludeFromCodeCoverage]
public sealed record RawOrder(
    string Source,
    string ExternalOrderId,
    IReadOnlyCollection<RawOrderItem> Items);
