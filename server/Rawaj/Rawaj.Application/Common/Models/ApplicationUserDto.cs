using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Models;

public class ApplicationUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public Language PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
}
