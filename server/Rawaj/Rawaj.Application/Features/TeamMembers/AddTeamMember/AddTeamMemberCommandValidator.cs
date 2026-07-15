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
    }
}
