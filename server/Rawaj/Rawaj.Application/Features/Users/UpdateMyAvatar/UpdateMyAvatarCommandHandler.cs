using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.UpdateMyAvatar;

public class UpdateMyAvatarCommandHandler(
    ILocalImageStorageService imageStorageService, IIdentityService identityService, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateMyAvatarCommand, Result<UpdateMyAvatarResponse>>
{
    private const string AvatarsSubfolder = "avatars";

    public async Task<Result<UpdateMyAvatarResponse>> Handle(UpdateMyAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var avatarUrl = await imageStorageService.SaveAsync(request.Content, request.ContentType, AvatarsSubfolder, cancellationToken);

        var updated = await identityService.UpdateAvatarAsync(userId, avatarUrl, cancellationToken);
        if (!updated)
        {
            return Result<UpdateMyAvatarResponse>.Failure("User not found.");
        }

        return Result<UpdateMyAvatarResponse>.Success(new UpdateMyAvatarResponse(avatarUrl));
    }
}
