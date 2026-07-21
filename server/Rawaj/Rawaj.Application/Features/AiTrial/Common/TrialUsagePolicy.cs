using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Application.Features.AiTrial.Common;

public static class TrialUsagePolicy
{
    public const int DailyLimit = 5;

    public static Task<int> CountTodayAsync(IApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;

        return dbContext.AiJobs.CountAsync(
            j => j.TriggeredBy == userId && j.BrandProfileId == null && j.CreatedAt >= todayStart,
            cancellationToken);
    }
}
