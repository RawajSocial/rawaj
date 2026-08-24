using FluentValidation;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.UpdateTeamMember;

public class UpdateTeamMemberCommandValidator : AbstractValidator<UpdateTeamMemberCommand>
{
    public UpdateTeamMemberCommandValidator()
    {
        RuleFor(x => x.TenantMemberId).NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(TenantMemberRole.Owner)
            .WithMessage("A tenant can only have one owner, assigned at creation.");

        RuleFor(x => x.BrandProfileIds)
            .Must(x => x.Count > 0)
            .WithMessage("Select at least one brand profile for this role.")
            .When(x => x.Role is TenantMemberRole.Editor or TenantMemberRole.Viewer);
    }
}
