namespace Rawaj.Application.Common.Models;

public record SocialPublishRequest(
    string AccessToken,
    string AccountIdExternal,
    string Message,
    byte[]? ImageBytes,
    string? ImageContentType,
    DateTime? ScheduledAt);
