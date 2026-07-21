namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Marks a request as scoped to a single brand profile. Checked by BrandAccessAuthorizationBehavior
/// after TenantAuthorizationBehavior has already validated tenant membership and role - Owner/Admin
/// always pass, Editor/Viewer must have a matching TenantMemberBrandAccess row.
/// </summary>
public interface IRequireBrandAccess
{
    Guid BrandProfileId { get; }
}
