using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.GetPostingTimeSuggestions;

public record PostingTimeSuggestionDto(SocialPlatform Platform, DayOfWeek DayOfWeek, int Hour, bool FromHistoricalData);
