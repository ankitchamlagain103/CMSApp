using Application.Roles.Commands;
using FluentValidation;

namespace Application.Roles.Validators
{
    public class SyncRoleMenuClaimsCommandValidator : AbstractValidator<SyncRoleMenuClaimsCommand>
    {
        public SyncRoleMenuClaimsCommandValidator()
        {
            RuleFor(command => command.MenuIds)
                .NotNull();

            RuleForEach(command => command.MenuIds)
                .GreaterThan(0);
        }
    }
}
