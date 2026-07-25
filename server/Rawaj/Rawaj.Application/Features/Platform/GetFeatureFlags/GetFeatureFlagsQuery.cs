using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Platform.GetFeatureFlags;

public record GetFeatureFlagsQuery : IRequest<Result<FeatureFlagsResponse>>;

public record FeatureFlagsResponse(bool VideoGeneration, bool ExperimentalAi, bool CoinLedger);
