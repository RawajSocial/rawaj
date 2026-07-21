using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Auth.Logout;

public class LogoutCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<LogoutCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var revoked = await RefreshTokenPolicy.RevokeAsync(dbContext, request.RefreshToken, cancellationToken);

        if (revoked)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}
