using Application.Common.Models;
using Application.Employees.Commands;
using Application.Employees.Dtos;
using Application.Employees.Queries;
using Domain.Enums;

namespace Application.Employees
{
    public interface IEmployeeService
    {
        Task<CommonResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDto>> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<EmployeeDto>>> GetEmployeesAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDto>> UpdateEmployeeAsync(Guid id, UpdateEmployeeCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default);

        // Class/subject/section/period assignments (2026-08-06, moved here from the removed
        // ITeacherService -- TeacherId now FKs directly to Employee.Id).
        Task<CommonResponse<TeacherAssignmentDto>> AssignClassSubjectAsync(Guid employeeId, AssignTeacherCommand command, CancellationToken cancellationToken = default);

        // Optimized multi-section counterpart -- assigns the same ClassSubject/TimePeriodId to the
        // employee across several sections in one call instead of repeating the single-assignment
        // flow once per section.
        Task<CommonResponse<TeacherAssignmentBulkResultDto>> AssignClassSubjectBulkAsync(Guid employeeId, AssignTeacherBulkCommand command, CancellationToken cancellationToken = default);

        // General bulk-entry counterpart -- unlike AssignClassSubjectBulkAsync (one ClassSubject,
        // several sections), each Items row here carries its own ClassSubjectId/ClassSectionId/
        // TimePeriodId, so an employee's whole routine can be entered across several different
        // classes/subjects/sections/periods in one call.
        Task<CommonResponse<TeacherAssignmentBulkEntryResultDto>> AssignClassSubjectBulkEntryAsync(Guid employeeId, AssignTeacherBulkEntryCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveAssignmentAsync(Guid employeeId, Guid assignmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TeacherAssignmentDto>>> GetAssignmentsAsync(Guid employeeId, CancellationToken cancellationToken = default);

        // ID card preview (2026-08-06, moved here from the removed ITeacherService -- generalized
        // to any Employee, not just teaching staff).
        Task<CommonResponse<DocumentPreviewDto>> GetIdCardPreviewAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeSalaryDto>> AddSalaryAsync(Guid employeeId, AddEmployeeSalaryCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeSalaryDto>>> GetSalaryHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeTaxCalculationDto>> GetCurrentSalaryTaxCalculationAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeMonthlyTaxBreakdownDto>> GetMonthlySalaryTaxCalculationAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxPlanningDto>> GetTaxPlanningAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryAnnualForecastDto>> GetAnnualForecastAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxDetailsGridDto>> GetTaxDetailsGridAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryComponentDto>> AddSalaryComponentAsync(Guid employeeId, Guid salaryId, SalaryComponentInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSalaryComponentAsync(Guid employeeId, Guid salaryId, Guid componentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryDeductionDto>> AddSalaryDeductionAsync(Guid employeeId, Guid salaryId, SalaryDeductionInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSalaryDeductionAsync(Guid employeeId, Guid salaryId, Guid deductionId, CancellationToken cancellationToken = default);

        // 2026-07-23: code-driven counterpart of the two pairs above -- resolves Code against the
        // SalaryComponentType/DeductionType catalogs and dispatches to the matching table.
        Task<CommonResponse<SalaryLineDto>> AddSalaryLineAsync(Guid employeeId, Guid salaryId, SalaryLineInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSalaryLineAsync(Guid employeeId, Guid salaryId, Guid lineId, CancellationToken cancellationToken = default);

        Task<CommonResponse<InsurancePremiumDto>> AddInsurancePremiumAsync(Guid employeeId, Guid salaryId, InsurancePremiumInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveInsurancePremiumAsync(Guid employeeId, Guid salaryId, Guid premiumId, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentPreviewDto>> GetPayslipPreviewAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<PayslipSummaryDto>>> GetPayslipsAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayslipDetailDto>> GetPayslipDetailAsync(Guid employeeId, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryForecastDto>> GetSalaryForecastAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> RequestLoanAsync(Guid employeeId, RequestLoanCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeLoanDto>>> GetLoansAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> ApproveLoanAsync(Guid employeeId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> RejectLoanAsync(Guid employeeId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> CancelLoanAsync(Guid employeeId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryAdjustmentDto>> CreateSalaryAdjustmentAsync(Guid employeeId, CreateSalaryAdjustmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<BulkSalaryAdjustmentResultDto>> CreateBulkSalaryAdjustmentsAsync(CreateBulkSalaryAdjustmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<SalaryAdjustmentDto>>> GetSalaryAdjustmentsAsync(Guid employeeId, Guid? fiscalYearId, int? monthIndex, AdjustmentStatus? status, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryAdjustmentDto>> UpdateSalaryAdjustmentAsync(Guid employeeId, Guid adjustmentId, UpdateSalaryAdjustmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> CancelSalaryAdjustmentAsync(Guid employeeId, Guid adjustmentId, CancellationToken cancellationToken = default);

        // Qualifications and Documents (2026-07-23, moved here from ITeacherService -- neither
        // concept is teaching-specific, every employee can hold a degree or need an identity
        // document on file). No Teacher-side alias exists for these anymore.
        Task<CommonResponse<EmployeeQualificationDto>> AddQualificationAsync(Guid employeeId, AddEmployeeQualificationCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveQualificationAsync(Guid employeeId, Guid qualificationId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeQualificationDto>>> GetQualificationsAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDocumentDto>> UploadDocumentAsync(Guid employeeId, UploadEmployeeDocumentCommand command, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeDocumentDto>>> GetDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDocumentFileDto>> GetDocumentFileAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteDocumentAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default);

        // Document/qualification self-service upload + HR verification (2026-08-07). Self-service
        // ("My...") methods start the record Pending and are resolved from the caller's own
        // Employee record, same ResolveCurrentEmployeeIdAsync pattern as every other "Me" method.
        // Verify/Reject are permission-gated (granted to whichever role a school treats as "HR")
        // and one-shot -- only a Pending record can be decided.
        Task<CommonResponse<EmployeeQualificationDto>> AddMyQualificationAsync(AddEmployeeQualificationCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeQualificationDto>>> GetMyQualificationsAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeQualificationDto>> VerifyQualificationAsync(Guid employeeId, Guid qualificationId, QualificationVerificationCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeQualificationDto>> RejectQualificationAsync(Guid employeeId, Guid qualificationId, QualificationVerificationCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDocumentDto>> UploadMyDocumentAsync(UploadEmployeeDocumentCommand command, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeDocumentDto>>> GetMyDocumentsAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDocumentFileDto>> GetMyDocumentFileAsync(Guid documentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDocumentDto>> VerifyDocumentAsync(Guid employeeId, Guid documentId, DocumentVerificationCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDocumentDto>> RejectDocumentAsync(Guid employeeId, Guid documentId, DocumentVerificationCommand command, CancellationToken cancellationToken = default);

        // Profile photo (2026-07-23) -- single-file, same storage/download convention as
        // documents but only ever one per employee.
        Task<CommonResponse<bool>> UploadPhotoAsync(Guid employeeId, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDocumentFileDto>> GetPhotoFileAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeletePhotoAsync(Guid employeeId, CancellationToken cancellationToken = default);

        // Leave balances (2026-07-23).
        Task<CommonResponse<EmployeeLeaveBalanceDto>> AllocateLeaveBalanceAsync(Guid employeeId, AllocateLeaveBalanceCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeLeaveBalanceDto>>> GetLeaveBalancesAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        // Leave requests (2026-07-23) -- ManagerStatus/HrStatus each independently
        // Approve/Reject, HR not gated on Manager's decision (see LeaveRequest's own doc comment).
        Task<CommonResponse<LeaveRequestDto>> CreateLeaveRequestAsync(Guid employeeId, CreateLeaveRequestCommand command, Stream attachmentContent, string attachmentFileName, string attachmentContentType, long attachmentFileSizeBytes, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<LeaveRequestDto>>> GetLeaveRequestsAsync(Guid employeeId, LeaveApprovalStatus? managerStatus, LeaveApprovalStatus? hrStatus, bool? isPending, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveRequestDto>> GetLeaveRequestByIdAsync(Guid employeeId, Guid requestId, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveRequestDto>> ApproveManagerDecisionAsync(Guid employeeId, Guid requestId, LeaveDecisionCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveRequestDto>> RejectManagerDecisionAsync(Guid employeeId, Guid requestId, LeaveDecisionCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveRequestDto>> ApproveHrDecisionAsync(Guid employeeId, Guid requestId, LeaveDecisionCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveRequestDto>> RejectHrDecisionAsync(Guid employeeId, Guid requestId, LeaveDecisionCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> CancelLeaveRequestAsync(Guid employeeId, Guid requestId, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveSubstituteDto>> AddLeaveSubstituteAsync(Guid employeeId, Guid requestId, AddLeaveSubstituteCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveLeaveSubstituteAsync(Guid employeeId, Guid requestId, Guid substituteId, CancellationToken cancellationToken = default);

        // Composite Employee Profile page (2026-07-23).
        Task<CommonResponse<EmployeeProfileDto>> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken = default);

        // Composite "My Dashboard" page (2026-08-07) -- leave summary/pending requests, the
        // employee's class routine + best-effort "next class", and upcoming holidays/events.
        Task<CommonResponse<EmployeeDashboardDto>> GetEmployeeDashboardAsync(Guid employeeId, CancellationToken cancellationToken = default);

        // Portal account provisioning retrofit (2026-07-27) -- for an employee that didn't get a
        // login at creation time. 409 Conflict if one already exists.
        Task<CommonResponse<EmployeeDto>> RegisterUserAccountAsync(Guid employeeId, RegisterEmployeeUserAccountCommand command, CancellationToken cancellationToken = default);

        // Self-service "Me" endpoints (2026-08-06) -- no {id} route parameter, the caller's own
        // Employee record is resolved from ICurrentUserService. Available to any Employee-linked
        // login regardless of role (Teacher, Accountant, HR, Principal, ...) via
        // DefaultEnabledMenu, not a permission grant -- see EmployeeService's own doc comment.
        Task<CommonResponse<EmployeeProfileDto>> GetMyProfileAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeLeaveBalanceDto>>> GetMyLeaveBalancesAsync(Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveRequestDto>> CreateMyLeaveRequestAsync(CreateLeaveRequestCommand command, Stream attachmentContent, string attachmentFileName, string attachmentContentType, long attachmentFileSizeBytes, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<LeaveRequestDto>>> GetMyLeaveRequestsAsync(LeaveApprovalStatus? managerStatus, LeaveApprovalStatus? hrStatus, bool? isPending, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveRequestDto>> GetMyLeaveRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> CancelMyLeaveRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<PayslipSummaryDto>>> GetMyPayslipsAsync(Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayslipDetailDto>> GetMyPayslipDetailAsync(Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentPreviewDto>> GetMyPayslipPreviewAsync(Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxPlanningDto>> GetMyTaxPlanningAsync(Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxDetailsGridDto>> GetMyTaxDetailsGridAsync(Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TeacherAssignmentDto>>> GetMyAssignmentsAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeDashboardDto>> GetMyDashboardAsync(CancellationToken cancellationToken = default);
    }
}
