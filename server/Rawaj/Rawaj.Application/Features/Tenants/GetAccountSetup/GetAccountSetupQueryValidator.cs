using FluentValidation;

namespace Rawaj.Application.Features.Tenants.GetAccountSetup;

public class GetAccountSetupQueryValidator : AbstractValidator<GetAccountSetupQuery>
{
    public GetAccountSetupQueryValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
    }
}
