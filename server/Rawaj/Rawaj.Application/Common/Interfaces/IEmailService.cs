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
}
