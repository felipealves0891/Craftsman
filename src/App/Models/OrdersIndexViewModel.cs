namespace Craftsman.App.Models;

public sealed record OrdersIndexViewModel(
    IReadOnlyCollection<OrderListItemViewModel> Orders,
    ImportResultViewModel? LastImport);
