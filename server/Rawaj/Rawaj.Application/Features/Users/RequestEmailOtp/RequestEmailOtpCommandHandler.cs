using MediatR;
using Rawaj.Application.Common.Email;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Users.RequestEmailOtp;

public class RequestEmailOtpCommandHandler(
    IIdentityService identityService,
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : IRequestHandler<RequestEmailOtpCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RequestEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var user = await identityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return Result<bool>.Failure("Email is already verified.");
        }

        var issueResult = await EmailOtpPolicy.IssueAsync(dbContext, userId, user.Email, cancellationToken);
        if (!issueResult.Succeeded)
        {
            return Result<bool>.Failure(issueResult.ErrorMessage!);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var otpEmail = EmailTemplates.EmailVerificationOtp(issueResult.Data!);
        await emailService.SendEmailAsync(
            user.Email, otpEmail.Subject, otpEmail.Html, otpEmail.PlainText, cancellationToken);

        return Result<bool>.Success(true);
    }
}
