namespace Craftsman.Domain.Integration.Services;

public sealed record ImportFailure(string Source, string? ExternalOrderId, string Reason);
