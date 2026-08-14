using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Email;

/// <summary>
/// Sends email via Gmail's free SMTP relay (smtp.gmail.com:587, STARTTLS) using a Gmail account +
/// an App Password — Google requires an App Password rather than the regular account password
/// once 2-Step Verification is enabled (generate one at
/// https://myaccount.google.com/apppasswords). No paid email provider needed.
///
/// Deliverability note: sending a plain-text alternative, a real display name, and a
/// List-Unsubscribe header are the signals actually within this app's control. Whether the mail
/// lands in the inbox or spam ultimately depends on the sending domain's SPF/DKIM/DMARC records —
/// a personal Gmail account sending via SMTP has none of those to offer, so some spam-folder
/// placement should be expected until this sends from a real domain with DKIM configured.
/// </summary>
public partial class SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger) : IEmailService
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.Value.SenderEmail) && !string.IsNullOrWhiteSpace(settings.Value.SenderPassword);

    public async Task SendEmailAsync(
        string toEmail, string subject, string htmlBody, string? plainTextBody = null, CancellationToken cancellationToken = default)
    {
        // Nothing here — including building the MailMessage itself — may ever throw out of this
        // method: a misconfigured/placeholder sender address must not break the caller's actual
        // operation (e.g. a welcome email failing must never fail registration).
        try
        {
            var config = settings.Value;
            if (string.IsNullOrWhiteSpace(config.SenderEmail)
                || string.IsNullOrWhiteSpace(config.SenderPassword)
                || !MailAddress.TryCreate(config.SenderEmail, config.SenderName, out var fromAddress))
            {
                logger.LogWarning(
                    "An email was not sent — Email:SenderEmail/SenderPassword is not configured (or SenderEmail is still the placeholder value).");
                return;
            }

            using var client = new SmtpClient(config.SmtpHost, config.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(config.SenderEmail, config.SenderPassword),
            };

            using var message = new MailMessage
            {
                From = fromAddress,
                Subject = subject,
                Body = plainTextBody ?? StripHtml(htmlBody),
                IsBodyHtml = false,
            };
            message.To.Add(toEmail);
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
            message.Headers.Add("List-Unsubscribe", $"<mailto:{config.SenderEmail}?subject=unsubscribe>");

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send an email.");
        }
    }

    private static string StripHtml(string html) => HtmlTagRegex().Replace(html, string.Empty).Trim();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();
}
