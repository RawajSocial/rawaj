using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateContentItem;

public record GenerateContentItemCommand(
    Guid CampaignId,
    ContentType ContentType,
    SocialPlatform Platform,
    Language Language,
    string? Tone,
    string? AdditionalInstructions) : IRequest<Result<GenerateContentItemResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
