using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.ArchiveBrandProfile;

public class ArchiveBrandProfileCommandHandler(IBrandProfileService brandProfileService, ICurrentUserService currentUserService)
    : IRequestHandler<ArchiveBrandProfileCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ArchiveBrandProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<Unit>.Failure("Not authenticated.");
        }

        var result = await brandProfileService.ArchiveBrandProfileAsync(userId, request.BrandProfileId, cancellationToken);

        return result.Outcome switch
        {
            ArchiveBrandProfileOutcome.NotFound => Result<Unit>.Failure("Brand profile not found."),
            ArchiveBrandProfileOutcome.Forbidden => Result<Unit>.Failure("You do not have permission to manage this brand profile."),
            _ => Result<Unit>.Success(Unit.Value)
        };
    }
}
