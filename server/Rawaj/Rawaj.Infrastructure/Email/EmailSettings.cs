namespace Rawaj.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = null!;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = null!;
    public string SenderName { get; set; } = "Rawaj";
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
