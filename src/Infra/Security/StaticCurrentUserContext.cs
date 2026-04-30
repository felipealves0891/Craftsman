namespace Craftsman.Infra.Security;

public sealed class StaticCurrentUserContext : ICurrentUserContext
{
    private readonly CurrentUserInfo current;

    public StaticCurrentUserContext(CurrentUserInfo current)
    {
        this.current = current;
    }

    public CurrentUserInfo Current => current;
}
