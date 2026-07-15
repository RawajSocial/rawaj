using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.ScrapeWebsite;

public record ScrapeWebsiteCommand(Guid CompetitorId, string Url)
    : IRequest<Result<ScrapeWebsiteResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
