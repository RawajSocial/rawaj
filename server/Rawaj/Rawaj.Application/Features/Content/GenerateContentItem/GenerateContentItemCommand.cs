using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateContentItem;

public record GenerateContentItemCommand(
    Guid BrandProfileId,
    Guid? CampaignId,
    ContentType ContentType,
    SocialPlatform Platform,
    Language Language,
    string? Tone,
    string? AdditionalInstructions,
    ContentTemplateStyle TemplateStyle = ContentTemplateStyle.Auto) : IRequest<Result<GenerateContentItemResponse>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
