using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.UpdateMyAvatar;

public class UpdateMyAvatarCommandHandler(
    IMediaStorageService mediaStorageService, IIdentityService identityService, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateMyAvatarCommand, Result<UpdateMyAvatarResponse>>
{
    public async Task<Result<UpdateMyAvatarResponse>> Handle(UpdateMyAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // A deterministic, per-user public id (rather than a random one) means re-uploading an
        // avatar naturally overwrites the previous Cloudinary asset in place — no separate delete
        // step, no orphaned asset left behind.
        string avatarUrl;
        if (mediaStorageService.IsConfigured)
        {
            var upload = await mediaStorageService.UploadImageWithFixedIdAsync(
                request.Content, request.ContentType, $"avatars/{userId}", cancellationToken);
            avatarUrl = upload.Url;
        }
        else
        {
            avatarUrl = $"data:{request.ContentType};base64,{Convert.ToBase64String(request.Content)}";
        }

        var updated = await identityService.UpdateAvatarAsync(userId, avatarUrl, cancellationToken);
        if (!updated)
        {
            return Result<UpdateMyAvatarResponse>.Failure("User not found.");
        }

        return Result<UpdateMyAvatarResponse>.Success(new UpdateMyAvatarResponse(avatarUrl));
    }
}
