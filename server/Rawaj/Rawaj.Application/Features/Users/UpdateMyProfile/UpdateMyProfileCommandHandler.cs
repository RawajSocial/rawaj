using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.UpdateMyProfile;

public class UpdateMyProfileCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateMyProfileCommand, Result<UpdateMyProfileResponse>>
{
    public async Task<Result<UpdateMyProfileResponse>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var updated = await identityService.UpdateProfileAsync(
            userId, request.FullName, request.PreferredLanguage, request.AvatarUrl, cancellationToken);

        if (!updated)
        {
            return Result<UpdateMyProfileResponse>.Failure("User not found.");
        }

        return Result<UpdateMyProfileResponse>.Success(
            new UpdateMyProfileResponse(request.FullName, request.AvatarUrl, request.PreferredLanguage));
    }
}
