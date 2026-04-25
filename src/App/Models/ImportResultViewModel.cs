namespace Craftsman.App.Models;

public sealed record ImportResultViewModel(
    int ImportedCount,
    int SkippedCount,
    IReadOnlyCollection<string> Failures);
