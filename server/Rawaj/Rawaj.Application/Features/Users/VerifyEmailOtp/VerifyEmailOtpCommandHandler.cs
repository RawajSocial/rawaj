using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Users.VerifyEmailOtp;

public class VerifyEmailOtpCommandHandler(
    IIdentityService identityService,
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext)
    : IRequestHandler<VerifyEmailOtpCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var verifyResult = await EmailOtpPolicy.VerifyAsync(dbContext, userId, request.Code, cancellationToken);
        if (!verifyResult.Succeeded)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure(verifyResult.ErrorMessage!);
        }

        await identityService.MarkEmailConfirmedAsync(userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
