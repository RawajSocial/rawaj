using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Users.GetMyProfile;

namespace Rawaj.Application.Features.Users.UpdateMyProfile;

public class UpdateMyProfileCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateMyProfileCommand, Result<GetMyProfileResponse>>
{
    public async Task<Result<GetMyProfileResponse>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        if (request.Username is not null)
        {
            var existingUsername = await identityService.FindByUsernameAsync(request.Username, cancellationToken);
            if (existingUsername is not null && existingUsername.Id != userId)
            {
                return Result<GetMyProfileResponse>.Failure("A user with this username already exists.");
            }
        }

        var result = await identityService.UpdatePartialProfileAsync(
            userId, request.FullName, request.Username, request.PreferredLanguage, cancellationToken);

        if (!result.Succeeded)
        {
            return Result<GetMyProfileResponse>.Failure(string.Join(" ", result.Errors));
        }

        var updated = await identityService.FindByIdAsync(userId, cancellationToken);
        return Result<GetMyProfileResponse>.Success(
            new GetMyProfileResponse(updated!.Id, updated.Email, updated.Username, updated.FullName, updated.AvatarUrl, updated.PreferredLanguage));
    }
}
