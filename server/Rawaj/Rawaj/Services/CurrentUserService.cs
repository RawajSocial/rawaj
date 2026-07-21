using System.Security.Claims;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User
                .FindFirst(c => c.Type is "sub" or ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
