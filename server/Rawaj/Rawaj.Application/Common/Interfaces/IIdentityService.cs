using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<IdentityRegisterResult> CreateUserAsync(string email, string username, string password, string fullName, Language preferredLanguage, CancellationToken cancellationToken);

    Task<ApplicationUserDto?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<ApplicationUserDto?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<ApplicationUserDto?> FindByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken);

    Task<ApplicationUserDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<List<ApplicationUserDto>> FindByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    Task<IdentityUpdateProfileResult> UpdatePartialProfileAsync(Guid userId, string? fullName, string? username, Language? preferredLanguage, CancellationToken cancellationToken);

    Task<bool> UpdateAvatarAsync(Guid userId, string? avatarUrl, CancellationToken cancellationToken);

    Task<IdentityChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken);

    Task<(List<ApplicationUserDto> Users, int TotalCount)> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);

    Task<bool> MarkEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken);
}
