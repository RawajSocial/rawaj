using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.DeleteMyAvatar;

public class DeleteMyAvatarCommandHandler(
    IIdentityService identityService, IMediaStorageService mediaStorageService, ICurrentUserService currentUserService)
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

        // Avatars upload under a deterministic public id keyed by userId (see
        // UpdateMyAvatarCommandHandler), so it can be reconstructed here without storing it
        // separately. A "data:" URL means it was saved before Cloudinary was configured — nothing
        // to delete from storage in that case.
        if (!string.IsNullOrWhiteSpace(user.AvatarUrl)
            && mediaStorageService.IsConfigured
            && !user.AvatarUrl.StartsWith("data:", StringComparison.Ordinal))
        {
            await mediaStorageService.DeleteAsync($"avatars/{userId}", cancellationToken);
        }

        await identityService.UpdateAvatarAsync(userId, null, cancellationToken);

        return Result<bool>.Success(true);
    }
}
