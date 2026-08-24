namespace Rawaj.Application.Common.Validation;

/// <summary>
/// Users routinely type a website as "example.com" or "www.example.com" with no scheme, which
/// fails `Uri.TryCreate(url, UriKind.Absolute, ...)` outright. Normalizing to a default "https://"
/// scheme before validating/persisting accepts that everyday input instead of rejecting it with a
/// confusing "must be a valid absolute URL" error for a URL that looks perfectly valid to a user.
/// </summary>
public static class UrlNormalizer
{
    public static string? EnsureScheme(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var trimmed = url.Trim();
        return trimmed.Contains("://") ? trimmed : $"https://{trimmed}";
    }

    public static bool IsValidUrl(string? url) =>
        Uri.TryCreate(EnsureScheme(url), UriKind.Absolute, out _);
}
