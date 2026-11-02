using Craftsman.Domain.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Craftsman.Infra.Services;

public sealed class MemoryApplicationCache : IApplicationCache
{
    private readonly bool enabled;
    private readonly Dictionary<string, long> generations = [];
    private readonly HashSet<string> keys = [];
    private readonly IMemoryCache memoryCache;
    private readonly object sync = new();

    public MemoryApplicationCache(IMemoryCache memoryCache, IConfiguration configuration)
    {
        this.memoryCache = memoryCache;
        enabled = configuration.GetValue("Cache:Enabled", true);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return await factory(cancellationToken);
        }

        long generation;

        lock (sync)
        {
            if (memoryCache.TryGetValue(key, out var cachedValue))
            {
                return (T)cachedValue!;
            }

            keys.Add(key);
            generation = generations.GetValueOrDefault(key);
        }

        var created = await factory(cancellationToken);

        lock (sync)
        {
            if (generations.GetValueOrDefault(key) == generation)
            {
                memoryCache.Set(
                    key,
                    created,
                    absoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(10));
            }
        }

        return created;
    }

    public void Remove(string key)
    {
        lock (sync)
        {
            generations[key] = generations.GetValueOrDefault(key) + 1;
            keys.Remove(key);
            memoryCache.Remove(key);
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
                generations[key] = generations.GetValueOrDefault(key) + 1;
                keys.Remove(key);
                memoryCache.Remove(key);
            }
        }
    }
}
