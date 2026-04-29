namespace Craftsman.Infra.Integrations.Loggi;

public sealed record LoggiTrackingEvent(
    string TrackingCode,
    int? StatusCode,
    string? StatusDescription,
    DateTimeOffset? UpdatedAt);
