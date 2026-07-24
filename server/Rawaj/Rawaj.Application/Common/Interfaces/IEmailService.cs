namespace Rawaj.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// <paramref name="plainTextBody"/> should always be supplied when a template has one — a
    /// plain-text alternative alongside the HTML part is one of the biggest, most controllable
    /// signals for landing in the inbox instead of spam. If omitted, a naive HTML-tag-stripped
    /// fallback is sent instead (better than nothing, but a real plain-text version is preferred).
    /// </summary>
    Task SendEmailAsync(
        string toEmail, string subject, string htmlBody, string? plainTextBody = null, CancellationToken cancellationToken = default);

    /// <summary>Whether sender credentials are actually configured — when false,
    /// <see cref="SendEmailAsync"/> silently no-ops instead of sending. Callers that need to tell
    /// the user "this action succeeded but no email was sent" (e.g. a team invite) read this rather
    /// than inferring it from a lack of exception.</summary>
    bool IsConfigured { get; }
}
