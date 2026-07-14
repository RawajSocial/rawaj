using FluentValidation;

namespace Rawaj.Application.Features.Tenants.InviteMember;

public class InviteTenantMemberCommandValidator : AbstractValidator<InviteTenantMemberCommand>
{
    public InviteTenantMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Role).IsInEnum();
    }
}
