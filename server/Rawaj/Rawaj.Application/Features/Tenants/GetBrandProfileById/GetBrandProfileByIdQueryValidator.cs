using FluentValidation;

namespace Rawaj.Application.Features.Tenants.GetBrandProfileById;

public class GetBrandProfileByIdQueryValidator : AbstractValidator<GetBrandProfileByIdQuery>
{
    public GetBrandProfileByIdQueryValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
    }
}
