namespace Rawaj.Application.Common.Models;

public enum SendOtpOutcome
{
    Sent,
    RateLimited
}

public record SendOtpResult(SendOtpOutcome Outcome, DateTime? RetryAfter);

public enum VerifyOtpOutcome
{
    Success,
    Invalid,
    Expired,
    AlreadyConsumed,
    NotFound
}

public record VerifyOtpResult(VerifyOtpOutcome Outcome);
