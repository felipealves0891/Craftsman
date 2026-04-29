namespace Craftsman.Infra.Integrations.Loggi;

public enum LoggiApiErrorKind
{
    Authentication,
    Authorization,
    RateLimitOrTransient,
    InvalidResponse,
    Unknown
}

public sealed class LoggiApiException : Exception
{
    public LoggiApiException(LoggiApiErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public LoggiApiErrorKind Kind { get; }
}
