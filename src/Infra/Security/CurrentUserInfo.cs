namespace Craftsman.Infra.Security;

public sealed record CurrentUserInfo(
    int? UserId,
    string? UserName,
    IReadOnlyCollection<string> RoleNames,
    string? CorrelationId,
    string? RequestPath)
{
    public static CurrentUserInfo Anonymous(string? correlationId = null, string? requestPath = null)
        => new(null, null, Array.Empty<string>(), correlationId, requestPath);
}
