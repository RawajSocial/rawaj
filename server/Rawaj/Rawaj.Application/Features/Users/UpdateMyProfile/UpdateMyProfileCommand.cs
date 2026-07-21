using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Users.UpdateMyProfile;

public record UpdateMyProfileCommand(string FullName, Language PreferredLanguage, string? AvatarUrl)
    : IRequest<Result<UpdateMyProfileResponse>>;
