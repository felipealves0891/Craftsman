using System.Diagnostics.CodeAnalysis;
namespace Craftsman.Domain.Integration.Services;

[ExcludeFromCodeCoverage]
public sealed record OrderImportResult(int ImportedCount, int SkippedCount, IReadOnlyCollection<ImportFailure> Failures);
