namespace Craftsman.Domain.Sales.ObjectValues;

public sealed record OrderDiscount
{
    public string Description { get; }

    public Money Amount { get; }

    public OrderDiscount(string description, Money amount)
    {
        if (amount.Amount > 0 && string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Discount description is required when amount is greater than zero.", nameof(description));
        }

        Description = description.Trim();
        Amount = amount;
    }
}
