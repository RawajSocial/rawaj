using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Admin.GetUsers;

public record GetUsersQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<AdminUserSummary>>>;
