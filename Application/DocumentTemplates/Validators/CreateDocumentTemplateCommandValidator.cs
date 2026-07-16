using Application.DocumentTemplates.Commands;
using FluentValidation;

namespace Application.DocumentTemplates.Validators
{
    public class CreateDocumentTemplateCommandValidator : AbstractValidator<CreateDocumentTemplateCommand>
    {
        public CreateDocumentTemplateCommandValidator()
        {
            RuleFor(command => command.TemplateType)
                .IsInEnum();

            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.HtmlContent)
                .NotEmpty();
        }
    }
}
