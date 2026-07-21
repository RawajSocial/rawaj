using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Users.GetMyProfile;

public record GetMyProfileQuery : IRequest<Result<GetMyProfileResponse>>;
