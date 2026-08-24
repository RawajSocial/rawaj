namespace Rawaj.Infrastructure.Email;

/// <summary>
/// <see cref="SenderPassword"/> MUST be a Gmail **App Password** (16 characters, generated at
/// https://myaccount.google.com/apppasswords), never the account's normal login password — Gmail
/// requires 2-Step Verification to be enabled before an App Password can even be generated, and it
/// rejects a normal password for SMTP outright. Until both <see cref="SenderEmail"/> and
/// <see cref="SenderPassword"/> are set, <c>SmtpEmailService</c> silently no-ops (see
/// <c>IEmailService.IsConfigured</c>) rather than throwing, so a misconfigured mailer never breaks
/// the action that triggered the email (registration, a team invite, etc).
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Rawaj";
}
