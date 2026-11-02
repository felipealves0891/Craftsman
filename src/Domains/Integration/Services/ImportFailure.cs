using System.Diagnostics.CodeAnalysis;
namespace Craftsman.Domain.Integration.Services;

[ExcludeFromCodeCoverage]
public sealed record ImportFailure(string Source, string? ExternalOrderId, string Reason);
