using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Rawaj.Infrastructure.Resilience;

/// <summary>
/// Every outbound call to a third-party API (AI providers, social platforms) goes through one of
/// these named clients, so a network blip or a provider's transient 5xx no longer surfaces as a
/// hard failure on the first try, and a provider that's down for a while gets its calls
/// short-circuited instead of piling up slow, doomed requests.
/// </summary>
public static class ResilientHttpClientExtensions
{
    /// <summary>
    /// Registers a named HttpClient with retry (exponential backoff + jitter), a circuit breaker,
    /// and both per-attempt and total-request timeouts. <paramref name="attemptTimeout"/> and
    /// <paramref name="totalTimeout"/> are the standard resilience handler's defaults (10s/30s)
    /// unless overridden - AI generation calls in particular need much longer than that.
    /// </summary>
    public static void AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        TimeSpan? attemptTimeout = null,
        TimeSpan? totalTimeout = null,
        int retryCount = 2,
        Action<HttpClient>? configureClient = null)
    {
        var builder = configureClient is null
            ? services.AddHttpClient(name)
            : services.AddHttpClient(name, configureClient);

        builder.AddStandardResilienceHandler(options =>
        {
            if (attemptTimeout.HasValue)
            {
                options.AttemptTimeout.Timeout = attemptTimeout.Value;
            }

            if (totalTimeout.HasValue)
            {
                options.TotalRequestTimeout.Timeout = totalTimeout.Value;
            }

            options.Retry.MaxRetryAttempts = retryCount;

            // 429 is deliberately excluded from both retry and the circuit breaker. The AI
            // providers return it for quota exhaustion (Groq's tokens-per-day, HuggingFace's
            // monthly credits) with a Retry-After measured in *hours* - and since the retry
            // strategy honours Retry-After, retrying meant sleeping until TotalRequestTimeout
            // fired, turning an instantly-knowable "you're out of quota" into a two-minute hang
            // that surfaced as a generic "generation failed". Letting the 429 through unretried
            // lets the caller read the provider's own message and report it immediately. Quota
            // limits don't clear within a request's lifetime, so a retry could never have helped.
            options.Retry.ShouldHandle = static args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode != HttpStatusCode.TooManyRequests &&
                HttpClientResiliencePredicates.IsTransient(args.Outcome));

            // Likewise, one tenant burning the daily token budget must not open the breaker and
            // take down calls for everyone else.
            options.CircuitBreaker.ShouldHandle = static args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode != HttpStatusCode.TooManyRequests &&
                HttpClientResiliencePredicates.IsTransient(args.Outcome));

            // The circuit breaker's sampling window must be at least double the attempt timeout
            // (the resilience handler validates this), so it's derived rather than left default
            // whenever a longer attempt timeout is configured.
            var effectiveAttemptTimeout = attemptTimeout ?? options.AttemptTimeout.Timeout;
            var minSamplingDuration = effectiveAttemptTimeout * 2;
            if (options.CircuitBreaker.SamplingDuration < minSamplingDuration)
            {
                options.CircuitBreaker.SamplingDuration = minSamplingDuration;
            }
        });
    }
}
