using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Admin.SetUserActive;

public class SetUserActiveCommandHandler(IIdentityService identityService)
    : IRequestHandler<SetUserActiveCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var succeeded = await identityService.SetActiveAsync(request.UserId, request.IsActive, cancellationToken);

        return succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure("User not found.");
    }
}
