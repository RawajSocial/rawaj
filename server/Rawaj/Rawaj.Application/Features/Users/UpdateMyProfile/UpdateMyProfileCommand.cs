using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Users.GetMyProfile;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Users.UpdateMyProfile;

public record UpdateMyProfileCommand(string? FullName, string? Username, Language? PreferredLanguage)
    : IRequest<Result<GetMyProfileResponse>>;
