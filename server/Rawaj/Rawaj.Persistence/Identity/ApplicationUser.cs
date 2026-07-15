using Microsoft.AspNetCore.Identity;
using Rawaj.Domain.Enums;

namespace Rawaj.Persistence.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public Language PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
