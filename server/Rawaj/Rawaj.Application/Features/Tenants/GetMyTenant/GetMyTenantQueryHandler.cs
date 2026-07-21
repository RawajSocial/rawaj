using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.GetMyTenant;

public class GetMyTenantQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyTenantQuery, Result<GetMyTenantResponse>>
{
    public async Task<Result<GetMyTenantResponse>> Handle(GetMyTenantQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Result<GetMyTenantResponse>.Failure("Unauthorized.");
        }

        var response = await (
            from member in dbContext.TenantMembers
            join tenant in dbContext.Tenants on member.TenantId equals tenant.Id
            where member.UserId == userId && member.InvitationStatus == InvitationStatus.Accepted
            select new GetMyTenantResponse(tenant.Id, tenant.Name, tenant.Subdomain, tenant.TenantType, member.Role, tenant.IsActive)
        ).FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? Result<GetMyTenantResponse>.Failure("No organization found.")
            : Result<GetMyTenantResponse>.Success(response);
    }
}
