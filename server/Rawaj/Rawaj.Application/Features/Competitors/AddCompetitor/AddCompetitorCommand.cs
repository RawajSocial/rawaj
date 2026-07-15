using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.AddCompetitor;

public record AddCompetitorCommand(
    Guid BrandProfileId,
    string Name,
    string? Url,
    Dictionary<string, string>? SocialHandles,
    string? Notes) : IRequest<Result<AddCompetitorResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
