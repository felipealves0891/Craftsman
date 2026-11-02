using System.Diagnostics.CodeAnalysis;

namespace Craftsman.UI.Components.Shared;

[ExcludeFromCodeCoverage]
public sealed record SelectOption(string Value, string Text, bool Selected = false);
