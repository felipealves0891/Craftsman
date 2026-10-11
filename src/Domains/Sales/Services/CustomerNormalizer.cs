namespace Craftsman.Domain.Sales.Services;

public static class CustomerNormalizer
{
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var normalized = name
            .Where(character => !char.IsWhiteSpace(character) && !char.IsControl(character))
            .ToArray();

        return new string(normalized).ToUpperInvariant();
    }
}
