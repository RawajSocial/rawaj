using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ICurrentTenantContext
{
    Guid? TenantId { get; }
    TenantMemberRole? Role { get; }

    void Set(Guid tenantId, TenantMemberRole role);
}
