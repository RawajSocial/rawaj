using Rawaj.Infrastructure.Ai;
using Xunit;

namespace Rawaj.Application.Tests.Infrastructure;

/// <summary>
/// The gate that keeps several pipeline runs advancing concurrently (C16's worker) from firing an
/// unbounded number of simultaneous requests at one provider. Pure in-process semaphore logic, so it
/// is tested in isolation rather than through the orchestrator.
/// </summary>
public class AiProviderConcurrencyLimiterTests
{
    [Fact]
    public async Task ACallerBeyondTheLimit_WaitsUntilASlotIsReleased()
    {
        var limiter = new AiProviderConcurrencyLimiter();

        // HuggingFace's default limit is 2.
        var first = await limiter.AcquireAsync("HuggingFace", CancellationToken.None);
        var second = await limiter.AcquireAsync("HuggingFace", CancellationToken.None);

        var thirdTask = limiter.AcquireAsync("HuggingFace", CancellationToken.None);
        await Task.Delay(50);
        Assert.False(thirdTask.IsCompleted);

        first.Dispose();

        var third = await thirdTask;
        Assert.True(thirdTask.IsCompletedSuccessfully);

        second.Dispose();
        third.Dispose();
    }

    [Fact]
    public async Task DifferentProviders_DoNotShareASlot()
    {
        var limiter = new AiProviderConcurrencyLimiter();

        // Exhaust Tavily's limit (4) without ever touching Groq's.
        var tavilyLeases = new List<IDisposable>();
        for (var i = 0; i < 4; i++)
        {
            tavilyLeases.Add(await limiter.AcquireAsync("Tavily", CancellationToken.None));
        }

        var groqTask = limiter.AcquireAsync("Groq", CancellationToken.None);
        var completed = await Task.WhenAny(groqTask, Task.Delay(200));

        Assert.Same(groqTask, completed);
        (await groqTask).Dispose();

        foreach (var lease in tavilyLeases)
        {
            lease.Dispose();
        }
    }

    [Fact]
    public async Task DisposingTwice_ReleasesOnlyOnce()
    {
        // A double-dispose releasing the semaphore twice would let one extra caller through the
        // limit until the process restarts — a slow, silent leak of the guarantee this exists for.
        var limiter = new AiProviderConcurrencyLimiter();
        var leases = new List<IDisposable>
        {
            await limiter.AcquireAsync("HuggingFace", CancellationToken.None),
            await limiter.AcquireAsync("HuggingFace", CancellationToken.None)
        };

        leases[0].Dispose();
        leases[0].Dispose(); // double-dispose

        var reacquired = await limiter.AcquireAsync("HuggingFace", CancellationToken.None);
        var blockedTask = limiter.AcquireAsync("HuggingFace", CancellationToken.None);
        await Task.Delay(50);

        Assert.False(blockedTask.IsCompleted);

        reacquired.Dispose();
        leases[1].Dispose();
        (await blockedTask).Dispose();
    }

    [Fact]
    public async Task AnUnlistedProvider_StillGetsThrottled_RatherThanBeingUnbounded()
    {
        var limiter = new AiProviderConcurrencyLimiter();
        var leases = new List<IDisposable>();

        for (var i = 0; i < 2; i++)
        {
            leases.Add(await limiter.AcquireAsync("SomeFutureProvider", CancellationToken.None));
        }

        var thirdTask = limiter.AcquireAsync("SomeFutureProvider", CancellationToken.None);
        await Task.Delay(50);
        Assert.False(thirdTask.IsCompleted);

        leases[0].Dispose();
        (await thirdTask).Dispose();
        leases[1].Dispose();
    }
}
