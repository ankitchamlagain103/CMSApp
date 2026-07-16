using Application.Students.Commands;
using FluentValidation;

namespace Application.Students.Validators
{
    public class UploadStudentDocumentCommandValidator : AbstractValidator<UploadStudentDocumentCommand>
    {
        public UploadStudentDocumentCommandValidator()
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
