using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Platform.GetFeatureFlags;

public class GetFeatureFlagsQueryHandler(IFeatureFlags featureFlags)
    : IRequestHandler<GetFeatureFlagsQuery, Result<FeatureFlagsResponse>>
{
    public Task<Result<FeatureFlagsResponse>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var response = new FeatureFlagsResponse(
            featureFlags.VideoGeneration, featureFlags.ExperimentalAi, featureFlags.CoinLedger);

        return Task.FromResult(Result<FeatureFlagsResponse>.Success(response));
    }
}
