namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Bounds how many pipeline stages may be mid-call to a given external provider at once, across the
/// whole process — not per run, not per DbContext. The worker (C16) can advance several runs
/// concurrently, each in its own scope, so without this a burst of runs all reaching a Groq stage in
/// the same tick would fire an unbounded number of simultaneous requests at one provider.
///
/// <para>Registered as a singleton: the limit only means anything if every concurrent run shares the
/// same semaphores.</para>
/// </summary>
public interface IAiProviderConcurrencyLimiter
{
    /// <summary>Blocks until a slot for <paramref name="provider"/> is free, then returns a token that
    /// releases it on <see cref="IDisposable.Dispose"/>. Callers should wrap the provider call in a
    /// <c>using</c>/<c>try-finally</c> so a slot is never leaked on an exception.</summary>
    Task<IDisposable> AcquireAsync(string provider, CancellationToken cancellationToken);
}
