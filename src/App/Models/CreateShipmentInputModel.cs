using System.ComponentModel.DataAnnotations;

namespace Craftsman.App.Models;

public sealed class CreateShipmentInputModel
{
    [Required]
    public Guid OrderId { get; set; }

    [StringLength(150)]
    public string? TrackingCode { get; set; }

    public IReadOnlyCollection<ShipmentOrderSelectionViewModel> AvailableOrders { get; set; } =
        Array.Empty<ShipmentOrderSelectionViewModel>();
}
