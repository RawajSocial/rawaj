using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.VerifyOtp;

public record VerifyOtpCommand(string Code) : IRequest<Result<Unit>>;
