using Rawaj.Domain.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Domain.Entities.Identity;

public class EmailOtp : BaseEntity
{
    public Guid UserId { get; set; }
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public int SendCount { get; set; }
    public DateTime WindowStartAt { get; set; }
    public DateTime LastSentAt { get; set; }
}
