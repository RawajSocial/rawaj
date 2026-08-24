using Rawaj.Application.Features.Scheduling.SchedulePost;
using Xunit;

namespace Rawaj.Application.Tests.Features.Scheduling;

public class SchedulePostCommandValidatorTests
{
    private readonly SchedulePostCommandValidator _validator = new();

    [Fact]
    public void Validate_WithScheduledAtLessThanTenMinutesOut_Fails()
    {
        var command = new SchedulePostCommand(
            Guid.NewGuid(), null, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SchedulePostCommand.ScheduledAt));
    }

    [Fact]
    public void Validate_WithScheduledAtExactlyTenMinutesOut_Passes()
    {
        var command = new SchedulePostCommand(
            Guid.NewGuid(), null, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(11));

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyContentItemId_Fails()
    {
        var command = new SchedulePostCommand(
            Guid.Empty, null, Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SchedulePostCommand.ContentItemId));
    }
}
