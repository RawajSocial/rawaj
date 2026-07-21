using System.Diagnostics;

namespace Rawaj.Middleware;

/// <summary>
/// Emits one structured log line per request (method, path, status, duration, user id when
/// authenticated) as the minimum viable request observability - enough to grep/aggregate without
/// a dedicated APM sink, which needs credentials nobody's configured yet.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value
            : null;

        logger.Log(
            context.Response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Information,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms (user: {UserId})",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            userId ?? "anonymous");
    }
}
