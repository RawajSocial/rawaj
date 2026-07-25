using Rawaj.Application.Common.Interfaces;
using Rawaj.Domain.Entities.Tenants;

namespace Rawaj.Application.Common.Policies;

/// <summary>
/// Marks a user's lightweight personal account setup complete — today, that's just "they finished
/// registration" (self-signup or invite-accept-register both collect the same fields), so this is
/// called once, right at account creation, from both RegisterCommandHandler and
/// AcceptInvitationAndRegisterCommandHandler. Matches the single-SaveChanges-per-request pattern
/// used elsewhere — callers add via their own dbContext and rely on their own SaveChangesAsync.
/// </summary>
public static class AccountSetupPolicy
{
    private const string RegistrationStep = "registration";

    public static void MarkCompleted(IApplicationDbContext dbContext, Guid userId)
    {
        var now = DateTime.UtcNow;
        dbContext.AccountSetups.Add(new AccountSetup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 1,
            CompletedSteps = RegistrationStep,
            CompletedAt = now,
            CreatedAt = now
        });
    }
}
