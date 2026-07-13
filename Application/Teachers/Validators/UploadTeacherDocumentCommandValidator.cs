using Application.Teachers.Commands;
using FluentValidation;

namespace Application.Teachers.Validators
{
    public class UploadTeacherDocumentCommandValidator : AbstractValidator<UploadTeacherDocumentCommand>
    {
        public UploadTeacherDocumentCommandValidator()
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
