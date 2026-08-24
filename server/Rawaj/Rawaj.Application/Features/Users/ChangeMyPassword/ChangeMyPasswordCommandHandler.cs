using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.ChangeMyPassword;

public class ChangeMyPasswordCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    : IRequestHandler<ChangeMyPasswordCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var result = await identityService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);

        return result.Succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(string.Join(" ", result.Errors));
    }
}
