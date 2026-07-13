using Application.Menus.Queries;
using Domain.Constants;
using FluentValidation;

namespace Application.Menus.Validators
{
    public class GetMenusQueryValidator : AbstractValidator<GetMenusQuery>
    {
        public GetMenusQueryValidator()
        {
            RuleFor(query => query.MenuType)
                .Must(menuType => MenuTypes.All.Contains(menuType))
                    .WithMessage("MenuType must be one of: " + string.Join(", ", MenuTypes.All) + ".")
                .When(query => !string.IsNullOrEmpty(query.MenuType));

            RuleFor(query => query.MenuFor)
                .Must(menuFor => MenuAudience.All.Contains(menuFor))
                    .WithMessage("MenuFor must be one of: " + string.Join(", ", MenuAudience.All) + ".")
                .When(query => !string.IsNullOrEmpty(query.MenuFor));
        }
    }
}
