using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Admin.GetPlatformStats;

public record GetPlatformStatsQuery : IRequest<Result<PlatformStatsResponse>>;
