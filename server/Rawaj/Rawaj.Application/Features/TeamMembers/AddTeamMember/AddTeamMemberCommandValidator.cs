using FluentValidation;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AddTeamMember;

public class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(TenantMemberRole.Owner)
            .WithMessage("A tenant can only have one owner, assigned at creation.");

        // Owner/Admin manage every brand in the tenant by default, so they don't need explicit
        // brand assignment - Editor/Viewer are restricted to whatever brands they're invited to.
        RuleFor(x => x.BrandProfileIds)
            .Must(x => x.Count > 0)
            .WithMessage("Select at least one brand profile for this role.")
            .When(x => x.Role is TenantMemberRole.Editor or TenantMemberRole.Viewer);
    }
}
