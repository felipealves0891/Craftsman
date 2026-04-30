namespace Craftsman.App.Security;

public sealed class IdentitySeedOptions
{
    public const string SectionName = "IdentitySeed";

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;
}
