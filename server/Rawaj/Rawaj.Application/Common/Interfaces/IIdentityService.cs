using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<IdentityRegisterResult> CreateUserAsync(
        string email,
        string userName,
        string password,
        string fullName,
        Language preferredLanguage,
        CancellationToken cancellationToken);

    Task<ApplicationUserDto?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<ApplicationUserDto?> FindByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken);
}
