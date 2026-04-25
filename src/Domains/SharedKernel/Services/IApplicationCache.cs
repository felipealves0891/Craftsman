namespace Craftsman.Domain.Services;

public interface IApplicationCache
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default);

    void Remove(string key);

    void RemoveByPrefix(string prefix);
}
