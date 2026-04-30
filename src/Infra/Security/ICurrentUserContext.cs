namespace Craftsman.Infra.Security;

public interface ICurrentUserContext
{
    CurrentUserInfo Current { get; }
}
