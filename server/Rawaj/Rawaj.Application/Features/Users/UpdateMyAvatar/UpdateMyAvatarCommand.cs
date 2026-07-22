using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.UpdateMyAvatar;

public record UpdateMyAvatarCommand(byte[] Content, string ContentType, string FileName)
    : IRequest<Result<UpdateMyAvatarResponse>>;
