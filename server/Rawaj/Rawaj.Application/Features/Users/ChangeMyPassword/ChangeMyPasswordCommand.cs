using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.ChangeMyPassword;

public record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result<bool>>;
