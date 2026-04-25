namespace Craftsman.Domain.Sales.ObjectValues;

public sealed record CustomerInfo
{
    public string Name { get; }

    public string? Email { get; }

    public CustomerInfo(string name, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        Name = name.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
