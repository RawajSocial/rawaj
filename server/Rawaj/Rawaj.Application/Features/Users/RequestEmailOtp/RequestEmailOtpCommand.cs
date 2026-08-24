using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.RequestEmailOtp;

public record RequestEmailOtpCommand : IRequest<Result<bool>>;
