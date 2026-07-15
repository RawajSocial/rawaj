using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Infrastructure.SocialOAuth;

public class OAuthStateStore(IMemoryCache cache) : IOAuthStateStore
{
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

    public string Create(OAuthStateContext context)
    {
        var state = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

        cache.Set(CacheKey(state), context, Expiry);

        return state;
    }

    public OAuthStateContext? Consume(string state)
    {
        var key = CacheKey(state);

        if (!cache.TryGetValue(key, out OAuthStateContext? context))
        {
            return null;
        }

        cache.Remove(key);
        return context;
    }

    private static string CacheKey(string state) => $"oauth-state:{state}";
}
