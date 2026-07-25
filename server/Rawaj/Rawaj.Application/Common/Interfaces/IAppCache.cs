namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Thin abstraction over an in-process cache, used for genuinely static/slow-changing reads
/// (subscription plans, coin pricing) so the Application layer doesn't take a direct dependency
/// on a specific caching package. Single-instance only today, same as the SignalR hub — no
/// distributed cache is wired in until the app is actually horizontally scaled.
/// </summary>
public interface IAppCache
{
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory);
    void Remove(string key);
}
