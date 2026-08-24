using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.GenerateOnboardingQuestions;

/// <summary>
/// Generates a handful of tailored follow-up questions (with quick-reply suggestions) from
/// whatever onboarding answers have been collected so far, for the wizard's final "AI Strategist"
/// step. OnboardingContextJson is the raw JSON blob of the wizard's in-progress answers as
/// collected by the frontend - passed through rather than modeled field-by-field since the wizard
/// schema evolves independently of this endpoint and the model only needs it as context, not
/// structured data to act on.
/// </summary>
public record GenerateOnboardingQuestionsCommand(Guid BrandProfileId, string OnboardingContextJson)
    : IRequest<Result<GenerateOnboardingQuestionsResponse>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Viewer;
}
