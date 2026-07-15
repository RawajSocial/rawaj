using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Models;

public record OAuthStateContext(Guid UserId, Guid TenantId, Guid BrandProfileId, SocialPlatform Platform, string RedirectUri);
