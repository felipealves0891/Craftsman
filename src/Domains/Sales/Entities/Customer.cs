using Craftsman.Domain.Sales.Services;

namespace Craftsman.Domain.Sales.Entities;

public sealed class Customer
{
    public Guid Id { get; }

    public string Name { get; private set; }

    public string NormalizedName { get; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public Customer(
        Guid id,
        string name,
        string? phone = null,
        string? email = null,
        DateTimeOffset? createdAt = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(id));
        }

        var normalizedName = CustomerNormalizer.NormalizeName(name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        NormalizedName = normalizedName;
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email);
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public void UpdateContact(string name, string? phone, string? email)
    {
        if (CustomerNormalizer.NormalizeName(name) != NormalizedName)
        {
            throw new InvalidOperationException("Customer normalized name cannot be changed.");
        }

        Name = name.Trim();
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
