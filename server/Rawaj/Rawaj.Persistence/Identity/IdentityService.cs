using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Persistence.Identity;

public class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<IdentityRegisterResult> CreateUserAsync(string email, string username, string password, string fullName, Language preferredLanguage, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = email,
            FullName = fullName,
            PreferredLanguage = preferredLanguage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? IdentityRegisterResult.Success(user.Id)
            : IdentityRegisterResult.Failure(TranslateErrors(result.Errors));
    }

    private static string[] TranslateErrors(IEnumerable<IdentityError> errors)
        => errors
            .Select(e => e.Code == "DuplicateUserName" ? "A user with this username already exists." : e.Description)
            .ToArray();

    public async Task<ApplicationUserDto?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : ToDto(user);
    }

    public async Task<ApplicationUserDto?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(username);
        return user is null ? null : ToDto(user);
    }

    public Task<ApplicationUserDto?> FindByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken)
        => identifier.Contains('@')
            ? FindByEmailAsync(identifier, cancellationToken)
            : FindByUsernameAsync(identifier, cancellationToken);

    public async Task<ApplicationUserDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToDto(user);
    }

    public async Task<List<ApplicationUserDto>> FindByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.ToArray();
        var users = await userManager.Users
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users.Select(ToDto).ToList();
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && user.IsActive && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityUpdateProfileResult> UpdatePartialProfileAsync(Guid userId, string? fullName, string? username, Language? preferredLanguage, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityUpdateProfileResult.Failure(["User not found."]);
        }

        if (fullName is not null) user.FullName = fullName;
        if (username is not null) user.UserName = username;
        if (preferredLanguage is not null) user.PreferredLanguage = preferredLanguage.Value;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? IdentityUpdateProfileResult.Success()
            : IdentityUpdateProfileResult.Failure(TranslateErrors(result.Errors));
    }

    public async Task<bool> UpdateAvatarAsync(Guid userId, string? avatarUrl, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return true;
    }

    public async Task<IdentityChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityChangePasswordResult.Failure(["User not found."]);
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        return result.Succeeded
            ? IdentityChangePasswordResult.Success()
            : IdentityChangePasswordResult.Failure(result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
    }

    public async Task<(List<ApplicationUserDto> Users, int TotalCount)> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = userManager.Users.OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users.Select(ToDto).ToList(), totalCount);
    }

    public async Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> MarkEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.EmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return true;
    }

    private static ApplicationUserDto ToDto(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email!,
        Username = user.UserName!,
        FullName = user.FullName,
        AvatarUrl = user.AvatarUrl,
        PreferredLanguage = user.PreferredLanguage,
        IsActive = user.IsActive,
        IsPlatformAdmin = user.IsPlatformAdmin,
        EmailConfirmed = user.EmailConfirmed
    };
}
