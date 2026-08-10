using System.Text.RegularExpressions;

namespace Rawaj.Application.Common.Services;

/// <summary>
/// Redacts structured PII — emails and phone numbers — from tenant-authored free text before it
/// reaches a prompt (brand profile fields, onboarding brief answers).
///
/// <para>Personal-name detection is deliberately out of scope: that needs NLP/entity recognition this
/// codebase doesn't have, and a regex guess at "looks like a name" would either miss most real names
/// or flag half the brand descriptions in the database as false positives (a brand founder's name is
/// routinely legitimate content, e.g. "من تأسيس سارة أحمد"). This covers what a plain pattern can
/// actually catch reliably: the realistic case of someone pasting contact info into a business
/// description or an onboarding answer.</para>
///
/// <para>Distinct from <see cref="UntrustedTextSanitizer"/>, which guards against the text being
/// read as instructions — this guards against the text carrying someone's personal contact details
/// into a prompt (and from there into <c>AiJob.InputParams</c>, where it would otherwise sit
/// unredacted in our own database). Run this first, then wrap the result if the caller also needs
/// injection containment.</para>
/// </summary>
public static partial class PiiRedactor
{
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailPattern();

    // Requires a leading '+' (international format) or '0' (local mobile format, e.g. Egyptian
    // 01xxxxxxxxx, Saudi 05xxxxxxxx) so this stays targeted at phone-shaped text rather than matching
    // any long digit run — a bare number with no such prefix (a budget figure, a date serialized
    // oddly) is left alone.
    [GeneratedRegex(@"(?<!\d)(?:\+\d{1,3}[\s.-]?(?:\d[\s.-]?){7,11}\d|0(?:\d[\s.-]?){8,11}\d)(?!\d)")]
    private static partial Regex PhonePattern();

    public static string Redact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        var withoutEmails = EmailPattern().Replace(text, "[REDACTED_EMAIL]");
        return PhonePattern().Replace(withoutEmails, "[REDACTED_PHONE]");
    }
}
