using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DocumentTemplates.Commands;
using Application.DocumentTemplates.Dtos;
using Application.DocumentTemplates.Queries;
using Application.DocumentTemplates.Validators;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.DocumentTemplates
{
    public class DocumentTemplateService : IDocumentTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateDocumentTemplateCommandValidator _createValidator;
        private readonly UpdateDocumentTemplateCommandValidator _updateValidator;

        public DocumentTemplateService(
            IUnitOfWork unitOfWork,
            CreateDocumentTemplateCommandValidator createValidator,
            UpdateDocumentTemplateCommandValidator updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<CommonResponse<DocumentTemplateDto>> CreateDocumentTemplateAsync(CreateDocumentTemplateCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<DocumentTemplateDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var alreadyExists = await _unitOfWork.DocumentTemplates.TemplateTypeExistsAsync(command.TemplateType, null, cancellationToken);
            if (alreadyExists)
            {
                var conflictResponse = CommonResponse<DocumentTemplateDto>.Fail(ResponseCodes.Conflict, "A template for '" + command.TemplateType + "' already exists. Update it instead.");
                return conflictResponse;
            }

            var documentTemplate = new DocumentTemplate
            {
                TemplateType = command.TemplateType,
                Name = command.Name.Trim(),
                HtmlContent = command.HtmlContent
            };

            await _unitOfWork.DocumentTemplates.AddAsync(documentTemplate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var documentTemplateDto = DocumentTemplateMapper.ToDto(documentTemplate);
            var successResponse = CommonResponse<DocumentTemplateDto>.Success(documentTemplateDto, "Document template created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<DocumentTemplateDto>> GetDocumentTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByIdAsync(id, cancellationToken);
            if (documentTemplate == null)
            {
                var notFoundResponse = CommonResponse<DocumentTemplateDto>.Fail(ResponseCodes.NotFound, "Document template with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var documentTemplateDto = DocumentTemplateMapper.ToDto(documentTemplate);
            var successResponse = CommonResponse<DocumentTemplateDto>.Success(documentTemplateDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<DocumentTemplateDto>>> GetDocumentTemplatesAsync(GetDocumentTemplatesQuery query, CancellationToken cancellationToken = default)
        {
            var pagedDocumentTemplates = await _unitOfWork.DocumentTemplates.GetPagedByFilterAsync(query.TemplateType, query.Page, query.PageSize, cancellationToken);

            var documentTemplateDtos = new List<DocumentTemplateDto>();
            foreach (var documentTemplate in pagedDocumentTemplates.Items)
            {
                var documentTemplateDto = DocumentTemplateMapper.ToDto(documentTemplate);
                documentTemplateDtos.Add(documentTemplateDto);
            }

            var paginatedResponse = new PaginatedResponse<DocumentTemplateDto>
            {
                Items = documentTemplateDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedDocumentTemplates.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<DocumentTemplateDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<DocumentTemplateDto>> UpdateDocumentTemplateAsync(Guid id, UpdateDocumentTemplateCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<DocumentTemplateDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByIdAsync(id, cancellationToken);
            if (documentTemplate == null)
            {
                var notFoundResponse = CommonResponse<DocumentTemplateDto>.Fail(ResponseCodes.NotFound, "Document template with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            documentTemplate.Name = command.Name.Trim();
            documentTemplate.HtmlContent = command.HtmlContent;

            _unitOfWork.DocumentTemplates.Update(documentTemplate);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var documentTemplateDto = DocumentTemplateMapper.ToDto(documentTemplate);
            var successResponse = CommonResponse<DocumentTemplateDto>.Success(documentTemplateDto, "Document template updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteDocumentTemplateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByIdAsync(id, cancellationToken);
            if (documentTemplate == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Document template with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            _unitOfWork.DocumentTemplates.Remove(documentTemplate);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Document template deleted successfully.");
            return successResponse;
        }

        public Task<CommonResponse<List<TemplatePlaceholderDto>>> GetPlaceholdersAsync(DocumentTemplateType templateType, CancellationToken cancellationToken = default)
        {
            var placeholders = BuildPlaceholders(templateType);
            var successResponse = CommonResponse<List<TemplatePlaceholderDto>>.Success(placeholders);
            return Task.FromResult(successResponse);
        }

        // The backend is the sole authority on what tokens exist per document type -- this
        // catalog is deliberately hardcoded, not itself admin-configurable.
        private static List<TemplatePlaceholderDto> BuildPlaceholders(DocumentTemplateType templateType)
        {
            var placeholders = new List<TemplatePlaceholderDto>();

            if (templateType == DocumentTemplateType.Payslip)
            {
                AddPlaceholder(placeholders, "EmployeeName", "Employee's full name.");
                AddPlaceholder(placeholders, "EmployeeCode", "Employee's unique code.");
                AddPlaceholder(placeholders, "JobPositionCode", "Employee's job position code.");
                AddPlaceholder(placeholders, "EffectiveFromDate", "Salary revision's effective-from date.");
                AddPlaceholder(placeholders, "FiscalYearCode", "Fiscal year used for the tax calculation.");
                AddPlaceholder(placeholders, "GrossMonthly", "Gross monthly pay.");
                AddPlaceholder(placeholders, "NetMonthly", "Net monthly pay after tax.");
                AddPlaceholder(placeholders, "GrossAnnualIncome", "Gross annual taxable income.");
                AddPlaceholder(placeholders, "RetirementContributionAnnual", "Annualized retirement contribution.");
                AddPlaceholder(placeholders, "RetirementExemption", "Retirement-fund exemption (least of three).");
                AddPlaceholder(placeholders, "InsuranceDeduction", "Capped insurance-premium deduction.");
                AddPlaceholder(placeholders, "AnnualTaxableIncome", "Taxable income after exemptions/deductions.");
                AddPlaceholder(placeholders, "AnnualTax", "Total annual tax.");
                AddPlaceholder(placeholders, "MonthlyTax", "Monthly tax.");
                AddPlaceholder(placeholders, "ComponentsRows", "Pre-built <tr> rows, one per salary component.");
                AddPlaceholder(placeholders, "DeductionsRows", "Pre-built <tr> rows, one per salary deduction.");
                AddPlaceholder(placeholders, "InsurancePremiumsRows", "Pre-built <tr> rows, one per insurance premium.");
                AddPlaceholder(placeholders, "TaxBreakdownRows", "Pre-built <tr> rows, one per tax slab.");
            }
            else if (templateType == DocumentTemplateType.FeeReceipt)
            {
                AddPlaceholder(placeholders, "StudentName", "Student's full name.");
                AddPlaceholder(placeholders, "AdmissionNo", "Student's admission number.");
                AddPlaceholder(placeholders, "GradeCode", "Enrolled grade code.");
                AddPlaceholder(placeholders, "SectionCode", "Enrolled section code.");
                AddPlaceholder(placeholders, "RollNumber", "Enrolled roll number.");
                AddPlaceholder(placeholders, "FeeItemsRows", "Pre-built <tr> rows, one per fee category.");
                AddPlaceholder(placeholders, "DiscountsRows", "Pre-built <tr> rows, one per discount.");
                AddPlaceholder(placeholders, "ScholarshipsRows", "Pre-built <tr> rows, one per scholarship.");
                AddPlaceholder(placeholders, "MonthlyRecurringTotal", "Total monthly recurring fees.");
                AddPlaceholder(placeholders, "AnnualTotal", "Total annual fees.");
                AddPlaceholder(placeholders, "OneTimeTotal", "Total one-time fees.");
                AddPlaceholder(placeholders, "RefundableDepositTotal", "Total refundable deposit.");
                AddPlaceholder(placeholders, "TotalDiscountReduction", "Total discount reduction against the monthly total.");
                AddPlaceholder(placeholders, "TotalScholarshipReduction", "Total scholarship reduction against the monthly total.");
                AddPlaceholder(placeholders, "NetMonthlyRecurring", "Monthly recurring total after discounts/scholarships.");
            }
            else if (templateType == DocumentTemplateType.StudentIdCard)
            {
                AddPlaceholder(placeholders, "StudentName", "Student's full name.");
                AddPlaceholder(placeholders, "AdmissionNo", "Student's admission number.");
                AddPlaceholder(placeholders, "GradeCode", "Enrolled grade code.");
                AddPlaceholder(placeholders, "SectionCode", "Enrolled section code.");
                AddPlaceholder(placeholders, "RollNumber", "Enrolled roll number.");
                AddPlaceholder(placeholders, "DateOfBirth", "Student's date of birth.");
                AddPlaceholder(placeholders, "GuardianName", "Primary guardian's full name.");
                AddPlaceholder(placeholders, "GuardianPhone", "Primary guardian's phone number.");
            }
            else if (templateType == DocumentTemplateType.TeacherIdCard)
            {
                AddPlaceholder(placeholders, "EmployeeCode", "Teacher's employee code.");
                AddPlaceholder(placeholders, "TeacherName", "Teacher's full name.");
                AddPlaceholder(placeholders, "JobPositionCode", "Teacher's job position code.");
                AddPlaceholder(placeholders, "TeachingLicenseNo", "Teacher's teaching license number.");
                AddPlaceholder(placeholders, "Specialization", "Teacher's specialization.");
                AddPlaceholder(placeholders, "JoinDate", "Teacher's join date.");
                AddPlaceholder(placeholders, "Phone", "Teacher's phone number.");
                AddPlaceholder(placeholders, "Email", "Teacher's email address.");
            }

            return placeholders;
        }

        private static void AddPlaceholder(List<TemplatePlaceholderDto> placeholders, string token, string description)
        {
            var placeholder = new TemplatePlaceholderDto
            {
                Token = token,
                Description = description
            };

            placeholders.Add(placeholder);
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
