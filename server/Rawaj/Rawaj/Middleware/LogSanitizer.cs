namespace Rawaj.Middleware;

/// <summary>
/// Strips CR/LF from values that end up in log messages but originate from the request (method,
/// path). Without this, a request could forge extra fake log lines by putting newlines in its
/// path - the values are still logged as-is otherwise, just without the line breaks.
/// </summary>
internal static class LogSanitizer
{
    public static string Sanitize(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\r", "").Replace("\n", "");
}
