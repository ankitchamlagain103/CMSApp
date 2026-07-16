using Application.DocumentTemplates.Commands;
using FluentValidation;

namespace Application.DocumentTemplates.Validators
{
    public class UpdateDocumentTemplateCommandValidator : AbstractValidator<UpdateDocumentTemplateCommand>
    {
        public UpdateDocumentTemplateCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(command => command.HtmlContent)
                .NotEmpty();
        }
    }
}
