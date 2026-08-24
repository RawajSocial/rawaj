using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface IRequireTenantRole
{
    TenantMemberRole MinimumRole { get; }
}
