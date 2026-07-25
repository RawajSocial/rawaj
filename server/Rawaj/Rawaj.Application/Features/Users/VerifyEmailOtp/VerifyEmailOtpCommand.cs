using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.VerifyEmailOtp;

public record VerifyEmailOtpCommand(string Code) : IRequest<Result<bool>>;
