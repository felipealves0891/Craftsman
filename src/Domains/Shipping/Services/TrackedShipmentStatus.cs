using Craftsman.Domain.Shipping.Entities;

namespace Craftsman.Domain.Shipping.Services;

public sealed record TrackedShipmentStatus(string TrackingCode, ShipmentStatus Status, DateTimeOffset TrackedAt);
