using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.DeleteMyAvatar;

public class DeleteMyAvatarCommandHandler(
    IIdentityService identityService, IAvatarStorageService avatarStorageService, ICurrentUserService currentUserService)
    : IRequestHandler<DeleteMyAvatarCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteMyAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var user = await identityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await avatarStorageService.DeleteAvatarAsync(user.AvatarUrl, cancellationToken);
        }

        await identityService.UpdateAvatarAsync(userId, null, cancellationToken);

        return Result<bool>.Success(true);
    }
}
