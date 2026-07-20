using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.SendOtp;

public record SendOtpCommand : IRequest<Result<Unit>>;
