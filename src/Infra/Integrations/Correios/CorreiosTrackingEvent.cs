using System.Diagnostics.CodeAnalysis;
namespace Craftsman.Infra.Integrations.Correios;

[ExcludeFromCodeCoverage]
public sealed record CorreiosTrackingEvent(
    string TrackingCode,
    string? Code,
    string? Type,
    string? Description,
    DateTimeOffset? CreatedAt);
