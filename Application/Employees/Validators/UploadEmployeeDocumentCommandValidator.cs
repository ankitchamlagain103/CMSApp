using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class UploadEmployeeDocumentCommandValidator : AbstractValidator<UploadEmployeeDocumentCommand>
    {
        public UploadEmployeeDocumentCommandValidator()
        {
            RuleFor(command => command.DocumentTypeCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.DocumentName)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}
