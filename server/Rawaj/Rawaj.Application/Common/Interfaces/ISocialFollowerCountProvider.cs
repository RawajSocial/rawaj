using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface ISocialFollowerCountProvider
{
    SocialPlatform Platform { get; }

    Task<FollowerCountResult> GetFollowerCountAsync(string accountIdExternal, string accessToken, CancellationToken cancellationToken);
}
