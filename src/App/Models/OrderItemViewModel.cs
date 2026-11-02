using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record OrderItemViewModel(
    Guid Id,
    string ExternalItemId,
    string Description,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    Guid? ProductId);
