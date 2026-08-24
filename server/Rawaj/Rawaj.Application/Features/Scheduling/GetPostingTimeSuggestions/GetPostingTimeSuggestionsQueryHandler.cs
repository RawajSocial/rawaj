using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Scheduling.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.GetPostingTimeSuggestions;

public class GetPostingTimeSuggestionsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetPostingTimeSuggestionsQuery, Result<List<PostingTimeSuggestionDto>>>
{
    private static readonly SocialPlatform[] AllPlatforms = Enum.GetValues<SocialPlatform>();

    public async Task<Result<List<PostingTimeSuggestionDto>>> Handle(
        GetPostingTimeSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var platforms = request.Platforms is { Count: > 0 } ? request.Platforms : AllPlatforms.ToList();

        var suggestions = await PostingTimeIntelligence.GetSuggestionsAsync(
            dbContext, request.BrandProfileId, platforms, cancellationToken);

        var dtos = suggestions
            .Select(s => new PostingTimeSuggestionDto(s.Platform, s.DayOfWeek, s.Hour, s.FromHistoricalData))
            .ToList();

        return Result<List<PostingTimeSuggestionDto>>.Success(dtos);
    }
}
