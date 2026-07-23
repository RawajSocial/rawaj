namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// The Application layer builds links (invite emails, etc.) but must not read configuration
/// directly, so this is the one seam Infrastructure exposes for "what's the frontend's base URL".
/// </summary>
public interface IFrontendUrlProvider
{
    string BaseUrl { get; }
}
