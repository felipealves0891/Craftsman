namespace Craftsman.Infra.Integrations.Shopee;

public enum ShopeeApiErrorKind
{
    Authentication,
    RateLimitOrTransient,
    InvalidResponse,
    Unknown
}

public sealed class ShopeeApiException : Exception
{
    public ShopeeApiException(ShopeeApiErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public ShopeeApiErrorKind Kind { get; }
}
