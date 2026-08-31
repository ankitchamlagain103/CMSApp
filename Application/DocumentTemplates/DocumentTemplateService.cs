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

        public async Task<CommonResponse<DocumentPreviewDto>> GetTemplatePreviewAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByIdAsync(id, cancellationToken);
            if (documentTemplate == null)
            {
                var notFoundResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "Document template with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var sampleValues = BuildSampleValues(documentTemplate.TemplateType);
            var renderedHtml = TemplateRenderer.Render(documentTemplate.HtmlContent, sampleValues);

            var documentPreviewDto = new DocumentPreviewDto
            {
                TemplateType = documentTemplate.TemplateType,
                Html = renderedHtml
            };

            var successResponse = CommonResponse<DocumentPreviewDto>.Success(documentPreviewDto);
            return successResponse;
        }

        // Sample values for every token BuildPlaceholders lists for that type -- same tokens, made
        // up but representative data, since a template-only preview has no real employee/student/
        // payment record behind it. Row tokens use the exact <tr> shape each real preview's own
        // Build*Rows helper produces (EmployeeService/EnrollmentService/FeePaymentService), just
        // with 1-2 sample rows instead of a real record's actual lines.
        private static Dictionary<string, string> BuildSampleValues(DocumentTemplateType templateType)
        {
            if (templateType == DocumentTemplateType.Payslip)
            {
                return new Dictionary<string, string>
                {
                    { "EmployeeName", "Sample Employee" },
                    { "EmployeeCode", "EMP2082001" },
                    { "JobPositionCode", "TEACHER" },
                    { "EffectiveFromDate", DateTime.Today.ToString("yyyy-MM-dd") },
                    { "FiscalYearCode", "2082/83" },
                    { "GrossMonthly", "50000.00" },
                    { "NetMonthly", "45666.67" },
                    { "GrossAnnualIncome", "600000.00" },
                    { "RetirementContributionAnnual", "60000.00" },
                    { "RetirementExemption", "60000.00" },
                    { "InsuranceDeduction", "20000.00" },
                    { "AnnualTaxableIncome", "520000.00" },
                    { "AnnualTax", "52000.00" },
                    { "MonthlyTax", "4333.33" },
                    {
                        "ComponentsRows",
                        "<tr><td>BASIC</td><td>FixedAmount</td><td>30000.00</td><td>Monthly</td></tr>" +
                        "<tr><td>DEARNESS_ALLOWANCE</td><td>FixedAmount</td><td>5000.00</td><td>Monthly</td></tr>"
                    },
                    { "DeductionsRows", "<tr><td>SSF_DEDUCTION</td><td>Percentage</td><td>11.00</td><td>Monthly</td></tr>" },
                    { "InsurancePremiumsRows", "<tr><td>Life</td><td>20000.00</td></tr>" },
                    {
                        "TaxBreakdownRows",
                        "<tr><td>1.00</td><td>500000.00</td><td>1.00%</td><td>499999.00</td><td>5000.00</td></tr>" +
                        "<tr><td>500000.00</td><td>-</td><td>10.00%</td><td>20000.00</td><td>2000.00</td></tr>"
                    }
                };
            }

            if (templateType == DocumentTemplateType.FeeReceipt)
            {
                return new Dictionary<string, string>
                {
                    { "StudentName", "Sample Student" },
                    { "AdmissionNo", "ADM2082001" },
                    { "GradeCode", "GRADE_5" },
                    { "SectionCode", "A" },
                    { "RollNumber", "12" },
                    {
                        "FeeItemsRows",
                        "<tr><td>TUITION_FEE</td><td>2000.00</td><td>Monthly</td><td>Compulsory</td></tr>" +
                        "<tr><td>COMPUTER_FEE</td><td>500.00</td><td>Monthly</td><td>Optional</td></tr>"
                    },
                    { "DiscountsRows", "<tr><td>SIBLING_DISCOUNT</td><td>10.00 (Percentage)</td></tr>" },
                    { "ScholarshipsRows", "<tr><td>MERIT_SCHOLARSHIP</td><td>500.00 (FixedAmount)</td></tr>" },
                    { "MonthlyRecurringTotal", "2500.00" },
                    { "AnnualInstallmentMonthlyShare", "0.00" },
                    { "AnnualTotal", "30000.00" },
                    { "OneTimeTotal", "5000.00" },
                    { "RefundableDepositTotal", "2000.00" },
                    { "TotalDiscountReduction", "250.00" },
                    { "TotalScholarshipReduction", "500.00" },
                    { "NetMonthlyRecurring", "1750.00" }
                };
            }

            if (templateType == DocumentTemplateType.PaymentReceipt)
            {
                return new Dictionary<string, string>
                {
                    { "SchoolName", "Sample School" },
                    { "SchoolAddress", "Kathmandu, Nepal" },
                    { "SchoolPhone", "01-4123456" },
                    { "ReceiptNo", "RCPT-2082-0001" },
                    { "PaymentDate", DateTime.Today.ToString("yyyy-MM-dd") },
                    { "StudentName", "Sample Student" },
                    { "AdmissionNo", "ADM2082001" },
                    { "GradeCode", "GRADE_5" },
                    { "SectionCode", "A" },
                    { "PaymentMode", "Cash" },
                    { "ReferenceNo", "(Ref 123456)" },
                    { "AmountPaid", "2500.00" },
                    { "Remarks", "Sample payment remarks" },
                    { "AllocationsRows", "<tr><td>INV-2082-001</td><td>Shrawan 2082</td><td>2500.00</td></tr>" },
                    {
                        "InvoiceLinesRows",
                        "<tr><td>1</td><td>INV-2082-001</td><td>Tuition Fee</td><td>2000.00</td></tr>" +
                        "<tr><td>2</td><td>INV-2082-001</td><td>Computer Fee</td><td>500.00</td></tr>"
                    },
                    { "OutstandingAmount", "0.00" }
                };
            }

            if (templateType == DocumentTemplateType.StudentIdCard)
            {
                return new Dictionary<string, string>
                {
                    { "StudentName", "Sample Student" },
                    { "AdmissionNo", "ADM2082001" },
                    { "GradeCode", "GRADE_5" },
                    { "SectionCode", "A" },
                    { "RollNumber", "12" },
                    { "DateOfBirth", "2015-04-10" },
                    { "GuardianName", "Sample Guardian" },
                    { "GuardianPhone", "9800000000" }
                };
            }

            if (templateType == DocumentTemplateType.TeacherIdCard)
            {
                return new Dictionary<string, string>
                {
                    { "EmployeeCode", "EMP2082001" },
                    { "TeacherName", "Sample Teacher" },
                    { "JobPositionCode", "TEACHER" },
                    { "TeachingLicenseNo", "TL-2082-001" },
                    { "Specialization", "Mathematics" },
                    { "JoinDate", "2082-01-01" },
                    { "Phone", "9800000000" },
                    { "Email", "sample.teacher@example.com" }
                };
            }

            return new Dictionary<string, string>();
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
                AddPlaceholder(placeholders, "MonthlyRecurringTotal", "Total monthly recurring fees, including the per-month share of any installment-split Annual item.");
                AddPlaceholder(placeholders, "AnnualInstallmentMonthlyShare", "Of MonthlyRecurringTotal, how much comes from installment-split Annual items.");
                AddPlaceholder(placeholders, "AnnualTotal", "Total annual fees (full yearly amount, installment-split or not).");
                AddPlaceholder(placeholders, "OneTimeTotal", "Total one-time fees.");
                AddPlaceholder(placeholders, "RefundableDepositTotal", "Total refundable deposit.");
                AddPlaceholder(placeholders, "TotalDiscountReduction", "Total discount reduction against the monthly total.");
                AddPlaceholder(placeholders, "TotalScholarshipReduction", "Total scholarship reduction against the monthly total.");
                AddPlaceholder(placeholders, "NetMonthlyRecurring", "Monthly recurring total after discounts/scholarships.");
            }
            else if (templateType == DocumentTemplateType.PaymentReceipt)
            {
                AddPlaceholder(placeholders, "SchoolName", "School name (AppConfig APP_NAME).");
                AddPlaceholder(placeholders, "SchoolAddress", "School address (AppConfig SCHOOL_ADDRESS).");
                AddPlaceholder(placeholders, "SchoolPhone", "School phone number (AppConfig SCHOOL_PHONE).");
                AddPlaceholder(placeholders, "ReceiptNo", "Payment receipt number.");
                AddPlaceholder(placeholders, "PaymentDate", "Date the payment was received.");
                AddPlaceholder(placeholders, "StudentName", "Student's full name.");
                AddPlaceholder(placeholders, "AdmissionNo", "Student's admission number.");
                AddPlaceholder(placeholders, "GradeCode", "Enrolled grade code.");
                AddPlaceholder(placeholders, "SectionCode", "Enrolled section code.");
                AddPlaceholder(placeholders, "PaymentMode", "How the payment was made (Cash/Cheque/...).");
                AddPlaceholder(placeholders, "ReferenceNo", "External reference (cheque/transaction) number, if any.");
                AddPlaceholder(placeholders, "AmountPaid", "Total amount received on this receipt.");
                AddPlaceholder(placeholders, "Remarks", "Payment remarks, if any.");
                AddPlaceholder(placeholders, "AllocationsRows", "Pre-built <tr> rows, one per invoice this payment settled against (invoice no, billing month, allocated amount).");
                AddPlaceholder(placeholders, "InvoiceLinesRows", "Pre-built <tr> rows, one per line of every allocated invoice, running Sr.No led (sr.no, invoice no, description, amount).");
                AddPlaceholder(placeholders, "OutstandingAmount", "The enrollment's remaining outstanding balance after this payment.");
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
