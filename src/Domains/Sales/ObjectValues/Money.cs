namespace Craftsman.Domain.Sales.ObjectValues;

public readonly record struct Money
{
    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency = "BRL") => new(0, currency);

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }
}
