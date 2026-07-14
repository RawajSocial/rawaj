using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Billing;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Persistence.Common;

namespace Rawaj.Persistence.Tenants;

public class TenantProvisioningService(
    AppDbContext dbContext,
    IIdentityService identityService,
    IEmailService emailService,
    IConfiguration configuration) : ITenantProvisioningService
{
    public async Task<Guid> ProvisionPrivateTenantAsync(Guid ownerUserId, string ownerUserName, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriptionPlanId = WellKnownIds.FreeSubscriptionPlanId,
            BillingCycle = BillingCycle.Monthly,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddYears(100),
            CreatedAt = now
        };
        dbContext.Subscriptions.Add(subscription);

        var subdomain = await GenerateUniqueSubdomainAsync(Slugify(ownerUserName), cancellationToken);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = $"{ownerUserName}'s Tenant",
            Subdomain = subdomain,
            TenantType = TenantType.Business,
            OwnerUserId = ownerUserId,
            SubscriptionId = subscription.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Tenants.Add(tenant);

        dbContext.TenantMembers.Add(new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = ownerUserId,
            Role = TenantMemberRole.Owner,
            InvitationStatus = InvitationStatus.Accepted,
            JoinedAt = now,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }

    public async Task InviteMemberAsync(Guid tenantId, string email, TenantMemberRole role, Guid invitedByUserId, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.SingleAsync(t => t.Id == tenantId, cancellationToken);
        var existingUser = await identityService.FindByEmailAsync(email, cancellationToken);
        var now = DateTime.UtcNow;
        Guid invitationId;

        if (existingUser is not null)
        {
            var existingMembership = await dbContext.TenantMembers
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == existingUser.Id, cancellationToken);

            if (existingMembership is not null)
            {
                return;
            }

            var member = new TenantMember
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = existingUser.Id,
                Role = role,
                InvitedBy = invitedByUserId,
                InvitationStatus = InvitationStatus.Pending,
                CreatedAt = now
            };
            dbContext.TenantMembers.Add(member);
            invitationId = member.Id;
        }
        else
        {
            var normalizedEmail = email.Trim().ToUpperInvariant();
            var alreadyInvited = await dbContext.TenantInvitations
                .AnyAsync(i => i.TenantId == tenantId && i.Status == InvitationStatus.Pending && i.Email.ToUpper() == normalizedEmail, cancellationToken);

            if (alreadyInvited)
            {
                return;
            }

            var invitation = new TenantInvitation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = email.Trim(),
                Role = role,
                InvitedBy = invitedByUserId,
                Status = InvitationStatus.Pending,
                CreatedAt = now
            };
            dbContext.TenantInvitations.Add(invitation);
            invitationId = invitation.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await SendInvitationEmailAsync(email, tenant.Name, role, invitationId, cancellationToken);
    }

    public async Task<InvitationDetailsDto?> GetInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await dbContext.TenantInvitations
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        if (invitation is not null)
        {
            return new InvitationDetailsDto(
                invitation.Id,
                invitation.TenantId,
                invitation.Tenant.Name,
                invitation.Role,
                invitation.Email,
                RequiresRegistration: true,
                invitation.Status);
        }

        var member = await dbContext.TenantMembers
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == invitationId, cancellationToken);

        if (member is null)
        {
            return null;
        }

        var user = await identityService.FindByIdAsync(member.UserId, cancellationToken);

        return new InvitationDetailsDto(
            member.Id,
            member.TenantId,
            member.Tenant.Name,
            member.Role,
            user?.Email ?? string.Empty,
            RequiresRegistration: false,
            member.InvitationStatus);
    }

    public async Task<InvitationActionOutcome> AcceptInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var invitation = await dbContext.TenantInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
        if (invitation is not null)
        {
            if (!string.Equals(invitation.Email.Trim(), currentUserEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return InvitationActionOutcome.NotAuthorized;
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return InvitationActionOutcome.AlreadyResolved;
            }

            invitation.Status = InvitationStatus.Accepted;
            invitation.RespondedAt = now;

            var alreadyMember = await dbContext.TenantMembers
                .AnyAsync(m => m.TenantId == invitation.TenantId && m.UserId == currentUserId, cancellationToken);

            if (!alreadyMember)
            {
                dbContext.TenantMembers.Add(new TenantMember
                {
                    Id = Guid.NewGuid(),
                    TenantId = invitation.TenantId,
                    UserId = currentUserId,
                    Role = invitation.Role,
                    InvitedBy = invitation.InvitedBy,
                    InvitationStatus = InvitationStatus.Accepted,
                    JoinedAt = now,
                    CreatedAt = now
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return InvitationActionOutcome.Success;
        }

        var member = await dbContext.TenantMembers.FirstOrDefaultAsync(m => m.Id == invitationId, cancellationToken);
        if (member is null)
        {
            return InvitationActionOutcome.NotFound;
        }

        if (member.UserId != currentUserId)
        {
            return InvitationActionOutcome.NotAuthorized;
        }

        if (member.InvitationStatus != InvitationStatus.Pending)
        {
            return InvitationActionOutcome.AlreadyResolved;
        }

        member.InvitationStatus = InvitationStatus.Accepted;
        member.JoinedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return InvitationActionOutcome.Success;
    }

    public async Task<InvitationActionOutcome> DeclineInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var invitation = await dbContext.TenantInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
        if (invitation is not null)
        {
            if (!string.Equals(invitation.Email.Trim(), currentUserEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return InvitationActionOutcome.NotAuthorized;
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return InvitationActionOutcome.AlreadyResolved;
            }

            invitation.Status = InvitationStatus.Declined;
            invitation.RespondedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return InvitationActionOutcome.Success;
        }

        var member = await dbContext.TenantMembers.FirstOrDefaultAsync(m => m.Id == invitationId, cancellationToken);
        if (member is null)
        {
            return InvitationActionOutcome.NotFound;
        }

        if (member.UserId != currentUserId)
        {
            return InvitationActionOutcome.NotAuthorized;
        }

        if (member.InvitationStatus != InvitationStatus.Pending)
        {
            return InvitationActionOutcome.AlreadyResolved;
        }

        member.InvitationStatus = InvitationStatus.Declined;
        await dbContext.SaveChangesAsync(cancellationToken);
        return InvitationActionOutcome.Success;
    }

    private async Task SendInvitationEmailAsync(string toEmail, string tenantName, TenantMemberRole role, Guid invitationId, CancellationToken cancellationToken)
    {
        var frontendBaseUrl = configuration["App:FrontendBaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var invitationLink = $"{frontendBaseUrl}/invitations/{invitationId}";

        var subject = $"You've been invited to join {tenantName} on Rawaj";

        var htmlBody = $"""
            <div style="font-family: Arial, Helvetica, sans-serif; max-width: 560px; margin: 0 auto; color: #1f2933;">
                <h2 style="color: #111827;">You're invited to join {tenantName}</h2>
                <p>Hello,</p>
                <p>You have been invited to join <strong>{tenantName}</strong> on Rawaj as a <strong>{role}</strong>.</p>
                <p style="margin: 28px 0;">
                    <a href="{invitationLink}" style="background-color: #4f46e5; color: #ffffff; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: bold;">
                        View Invitation
                    </a>
                </p>
                <p>If you already have a Rawaj account, you'll be asked to accept or decline this invitation. Otherwise, you'll be asked to create an account first.</p>
                <p style="margin-top: 24px;">If you weren't expecting this invitation, you can safely ignore this email.</p>
                <p style="margin-top: 32px; color: #6b7280; font-size: 13px;">— The Rawaj Team</p>
            </div>
            """;

        await emailService.SendAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    private async Task<string> GenerateUniqueSubdomainAsync(string baseSlug, CancellationToken cancellationToken)
    {
        var candidate = baseSlug;
        var suffix = 0;

        while (await dbContext.Tenants.AnyAsync(t => t.Subdomain == candidate, cancellationToken))
        {
            ++suffix;
            candidate = $"{baseSlug}-{suffix}";
        }

        return candidate;
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "tenant" : slug;
    }
}
