using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.DeleteMyAvatar;

public record DeleteMyAvatarCommand : IRequest<Result<bool>>;
