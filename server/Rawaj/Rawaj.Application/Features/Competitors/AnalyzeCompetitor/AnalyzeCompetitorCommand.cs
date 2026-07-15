using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.AnalyzeCompetitor;

public record AnalyzeCompetitorCommand(Guid CompetitorId)
    : IRequest<Result<AnalyzeCompetitorResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
