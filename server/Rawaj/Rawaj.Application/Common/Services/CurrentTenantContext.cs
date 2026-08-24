using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Services;

public class CurrentTenantContext : ICurrentTenantContext
{
    public Guid? TenantId { get; private set; }
    public TenantMemberRole? Role { get; private set; }

    public void Set(Guid tenantId, TenantMemberRole role)
    {
        TenantId = tenantId;
        Role = role;
    }
}
