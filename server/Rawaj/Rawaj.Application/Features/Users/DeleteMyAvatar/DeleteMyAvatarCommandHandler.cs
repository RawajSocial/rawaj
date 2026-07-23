using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.DeleteMyAvatar;

public class DeleteMyAvatarCommandHandler(
    IIdentityService identityService, ILocalImageStorageService imageStorageService, ICurrentUserService currentUserService)
    : IRequestHandler<DeleteMyAvatarCommand, Result<bool>>
{
    private const string AvatarsSubfolder = "avatars";

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
            await imageStorageService.DeleteAsync(user.AvatarUrl, AvatarsSubfolder, cancellationToken);
        }

        await identityService.UpdateAvatarAsync(userId, null, cancellationToken);

        return Result<bool>.Success(true);
    }
}
