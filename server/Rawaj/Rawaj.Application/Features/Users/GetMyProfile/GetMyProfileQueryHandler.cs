using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.GetMyProfile;

public class GetMyProfileQueryHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyProfileQuery, Result<GetMyProfileResponse>>
{
    public async Task<Result<GetMyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(currentUserService.UserId!.Value, cancellationToken);
        if (user is null)
        {
            return Result<GetMyProfileResponse>.Failure("User not found.");
        }

        return Result<GetMyProfileResponse>.Success(
            new GetMyProfileResponse(user.Id, user.Email, user.Username, user.FullName, user.AvatarUrl, user.PreferredLanguage, user.EmailConfirmed));
    }
}
