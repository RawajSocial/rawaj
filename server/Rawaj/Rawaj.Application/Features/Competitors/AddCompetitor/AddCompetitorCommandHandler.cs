using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.BrandIntelligence;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Competitors.AddCompetitor;

public class AddCompetitorCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<AddCompetitorCommand, Result<AddCompetitorResponse>>
{
    public async Task<Result<AddCompetitorResponse>> Handle(AddCompetitorCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var brandProfileBelongsToTenant = await dbContext.TenantBrandProfiles
            .AnyAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (!brandProfileBelongsToTenant)
        {
            return Result<AddCompetitorResponse>.Failure("Brand profile not found.");
        }

        var now = DateTime.UtcNow;

        var competitor = new Competitor
        {
            Id = Guid.NewGuid(),
            BrandProfileId = request.BrandProfileId,
            Name = request.Name,
            Url = request.Url,
            SocialHandles = request.SocialHandles ?? [],
            Notes = request.Notes,
            Ragged = false,
            Status = CompetitorStatus.Active,
            CreatedAt = now
        };

        dbContext.Competitors.Add(competitor);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AddCompetitorResponse>.Success(
            new AddCompetitorResponse(competitor.Id, competitor.BrandProfileId, competitor.Name));
    }
}
