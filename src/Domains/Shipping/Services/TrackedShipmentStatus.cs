using System.Diagnostics.CodeAnalysis;
using Craftsman.Domain.Shipping.Entities;

namespace Craftsman.Domain.Shipping.Services;

[ExcludeFromCodeCoverage]
public sealed record TrackedShipmentStatus(string TrackingCode, ShipmentStatus Status, DateTimeOffset TrackedAt);
