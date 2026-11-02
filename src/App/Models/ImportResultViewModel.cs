using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record ImportResultViewModel(
    int ImportedCount,
    int SkippedCount,
    IReadOnlyCollection<string> Failures);
