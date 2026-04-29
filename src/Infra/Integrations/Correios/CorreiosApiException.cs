namespace Craftsman.Infra.Integrations.Correios;

public enum CorreiosApiErrorKind
{
    Authentication,
    RateLimitOrTransient,
    InvalidResponse,
    Unknown
}

public sealed class CorreiosApiException : Exception
{
    public CorreiosApiException(CorreiosApiErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public CorreiosApiErrorKind Kind { get; }
}
