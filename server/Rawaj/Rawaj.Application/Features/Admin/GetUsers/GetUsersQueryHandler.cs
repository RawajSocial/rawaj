using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Admin.GetUsers;

public class GetUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, Result<PagedResult<AdminUserSummary>>>
{
    public async Task<Result<PagedResult<AdminUserSummary>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationDefaults.Clamp(request.Page, request.PageSize);

        var (users, totalCount) = await identityService.ListUsersAsync(page, pageSize, cancellationToken);

        var summaries = users
            .Select(u => new AdminUserSummary(u.Id, u.Email, u.FullName, u.IsActive, u.IsPlatformAdmin))
            .ToList();

        return Result<PagedResult<AdminUserSummary>>.Success(new PagedResult<AdminUserSummary>(summaries, page, pageSize, totalCount));
    }
}
