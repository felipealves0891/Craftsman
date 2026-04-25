using Craftsman.Domain.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Craftsman.Infra.Services;

public sealed class MemoryApplicationCache : IApplicationCache
{
    private readonly bool enabled;
    private readonly HashSet<string> keys = [];
    private readonly IMemoryCache memoryCache;
    private readonly object sync = new();

    public MemoryApplicationCache(IMemoryCache memoryCache, IConfiguration configuration)
    {
        this.memoryCache = memoryCache;
        enabled = configuration.GetValue("Cache:Enabled", true);
    }

    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return factory(cancellationToken);
        }

        lock (sync)
        {
            keys.Add(key);
        }

        return memoryCache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(10);

            return factory(cancellationToken);
        })!;
    }

    public void Remove(string key)
    {
        memoryCache.Remove(key);

        lock (sync)
        {
            keys.Remove(key);
        }
    }

    public void RemoveByPrefix(string prefix)
    {
        string[] matchingKeys;

        lock (sync)
        {
            matchingKeys = keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray();

            foreach (var key in matchingKeys)
            {
                keys.Remove(key);
            }
        }

        foreach (var key in matchingKeys)
        {
            memoryCache.Remove(key);
        }
    }
}
