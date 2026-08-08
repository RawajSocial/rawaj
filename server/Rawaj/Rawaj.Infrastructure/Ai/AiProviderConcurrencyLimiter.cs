using System.Collections.Concurrent;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Ai;

/// <summary>
/// One <see cref="SemaphoreSlim"/> per provider name, created lazily on first use and shared for the
/// life of the process. A provider not listed in <see cref="DefaultLimits"/> falls back to a
/// conservative default rather than throwing — a stage kind added to the graph later without a
/// corresponding limiter entry should degrade to "throttled a bit too eagerly", never "unthrottled".
/// </summary>
public class AiProviderConcurrencyLimiter : IAiProviderConcurrencyLimiter
{
    private static readonly Dictionary<string, int> DefaultLimits = new()
    {
        ["Groq"] = 4,
        ["Tavily"] = 4,
        ["Cloudflare"] = 2
    };

    private const int FallbackLimit = 2;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public async Task<IDisposable> AcquireAsync(string provider, CancellationToken cancellationToken)
    {
        var semaphore = _semaphores.GetOrAdd(provider, CreateSemaphore);
        await semaphore.WaitAsync(cancellationToken);
        return new Release(semaphore);
    }

    private static SemaphoreSlim CreateSemaphore(string provider)
    {
        var limit = DefaultLimits.GetValueOrDefault(provider, FallbackLimit);
        return new SemaphoreSlim(limit, limit);
    }

    private sealed class Release(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            // Guards against a double-dispose releasing the semaphore twice, which would let one
            // extra caller through the limit until the process restarts.
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                semaphore.Release();
            }
        }
    }
}
