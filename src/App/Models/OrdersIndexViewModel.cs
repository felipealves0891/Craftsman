using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record OrdersIndexViewModel(
    IReadOnlyCollection<OrderListItemViewModel> Orders,
    ImportResultViewModel? LastImport);
