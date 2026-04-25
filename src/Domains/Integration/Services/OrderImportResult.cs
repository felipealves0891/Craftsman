namespace Craftsman.Domain.Integration.Services;

public sealed record OrderImportResult(int ImportedCount, int SkippedCount, IReadOnlyCollection<ImportFailure> Failures);
