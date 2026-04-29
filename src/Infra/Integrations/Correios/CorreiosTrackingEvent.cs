namespace Craftsman.Infra.Integrations.Correios;

public sealed record CorreiosTrackingEvent(
    string TrackingCode,
    string? Code,
    string? Type,
    string? Description,
    DateTimeOffset? CreatedAt);
