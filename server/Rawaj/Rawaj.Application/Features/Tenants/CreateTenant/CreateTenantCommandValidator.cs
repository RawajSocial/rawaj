using FluentValidation;

namespace Rawaj.Application.Features.Tenants.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Subdomain may only contain lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.TenantType).IsInEnum();
    }
}
