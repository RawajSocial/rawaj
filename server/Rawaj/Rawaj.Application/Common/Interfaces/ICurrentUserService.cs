namespace Rawaj.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>
    /// The tenant the client wants to act in for this request, from the `X-Tenant-Id` header —
    /// used because a user can belong to more than one tenant (their own auto-provisioned tenant
    /// plus any tenant they've been invited into). Null means "let the server pick a default".
    /// </summary>
    Guid? RequestedTenantId { get; }
}
