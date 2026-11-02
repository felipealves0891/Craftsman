using System.Diagnostics.CodeAnalysis;
namespace Craftsman.Domain.Integration.Models;

[ExcludeFromCodeCoverage]
public sealed record RawOrderItem(
    string ExternalItemId,
    string Description,
    int Quantity,
    decimal UnitPrice);
