using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<IdentityRegisterResult> CreateUserAsync(
        string email,
        string password,
        string fullName,
        Language preferredLanguage,
        CancellationToken cancellationToken);

    Task<ApplicationUserDto?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<List<ApplicationUserDto>> FindByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken);

    Task<(List<ApplicationUserDto> Users, int TotalCount)> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
}
