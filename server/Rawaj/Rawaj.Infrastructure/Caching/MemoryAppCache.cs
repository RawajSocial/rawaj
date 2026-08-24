using Microsoft.Extensions.Caching.Memory;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Caching;

public class MemoryAppCache(IMemoryCache memoryCache) : IAppCache
{
    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        if (memoryCache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();
        memoryCache.Set(key, value, ttl);
        return value;
    }

    public void Remove(string key) => memoryCache.Remove(key);
}
