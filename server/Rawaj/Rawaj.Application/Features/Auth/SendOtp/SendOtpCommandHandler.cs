using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Auth.SendOtp;

public class SendOtpCommandHandler(IOtpService otpService, ICurrentUserService currentUserService)
    : IRequestHandler<SendOtpCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<Unit>.Failure("Not authenticated.");
        }

        var result = await otpService.SendAsync(userId, OtpPurpose.EmailVerification, cancellationToken);

        return result.Outcome switch
        {
            SendOtpOutcome.RateLimited => Result<Unit>.Failure("Too many OTP requests. Please try again later."),
            _ => Result<Unit>.Success(Unit.Value)
        };
    }
}
