using Rawaj.Domain.Common;

namespace Rawaj.Domain.Entities.Auth;

public class EmailOtpCode : BaseEntity
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public int Attempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
