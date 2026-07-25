using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Models;

public class ApplicationUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public Language PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public bool EmailConfirmed { get; set; }
}
