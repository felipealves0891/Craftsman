using System.Diagnostics.CodeAnalysis;
namespace Craftsman.Infra.Integrations.Loggi;

[ExcludeFromCodeCoverage]
public sealed record LoggiTrackingEvent(
    string TrackingCode,
    int? StatusCode,
    string? StatusDescription,
    DateTimeOffset? UpdatedAt);
