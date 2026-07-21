using FluentValidation;

namespace Rawaj.Application.Features.Content.RegenerateContentItem;

public class RegenerateContentItemCommandValidator : AbstractValidator<RegenerateContentItemCommand>
{
    public RegenerateContentItemCommandValidator()
    {
        RuleFor(x => x.ContentItemId).NotEmpty();
        RuleFor(x => x.Feedback).NotEmpty().MaximumLength(500);
    }
}
