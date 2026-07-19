using FluentValidation;

namespace Rawaj.Application.Features.Tenants.RemoveBrandImage;

public class RemoveBrandImageCommandValidator : AbstractValidator<RemoveBrandImageCommand>
{
    public RemoveBrandImageCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
    }
}
