using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Auth.VerifyOtp;

public class VerifyOtpCommandHandler(IOtpService otpService, ICurrentUserService currentUserService)
    : IRequestHandler<VerifyOtpCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<Unit>.Failure("Not authenticated.");
        }

        var result = await otpService.VerifyAsync(userId, OtpPurpose.EmailVerification, request.Code, cancellationToken);

        return result.Outcome switch
        {
            VerifyOtpOutcome.Success => Result<Unit>.Success(Unit.Value),
            VerifyOtpOutcome.NotFound => Result<Unit>.Failure("No verification code was requested."),
            VerifyOtpOutcome.Expired => Result<Unit>.Failure("This verification code has expired."),
            VerifyOtpOutcome.AlreadyConsumed => Result<Unit>.Failure("This verification code has already been used."),
            _ => Result<Unit>.Failure("Invalid verification code.")
        };
    }
}
