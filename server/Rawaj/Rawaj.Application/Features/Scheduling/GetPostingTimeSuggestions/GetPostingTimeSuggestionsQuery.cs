using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.GetPostingTimeSuggestions;

public record GetPostingTimeSuggestionsQuery(Guid BrandProfileId, List<SocialPlatform>? Platforms)
    : IRequest<Result<List<PostingTimeSuggestionDto>>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
