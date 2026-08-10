using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    // Aggregate repository: Employee plus its EmployeeSalary (and that salary's component/
    // deduction/insurance-premium children).
    public interface IEmployeeRepository : IRepository<Employee, Guid>
    {
        Task<PagedResult<Employee>> GetPagedByFilterAsync(EmployeeFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // Replaces the old GetByIdWithTeacherAsync (2026-08-06, Teacher entity removed --
        // TeachingLicenseNo/ExperienceYears/Specialization now live on this row directly, nothing
        // left to Include for them). Manager is still a real nav that needs an explicit Include.
        Task<Employee> GetByIdWithManagerAsync(Guid id, CancellationToken cancellationToken = default);

        // Self-service (2026-08-06) -- resolves "which Employee am I" from the caller's own
        // ApplicationUser id, for the "Me" endpoints in EmployeesController.
        Task<Employee> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<bool> EmployeeCodeExistsAsync(string employeeCode, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetEmployeeCodesByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

        Task<bool> UserIdExistsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<bool> HasSalariesAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EmployeeSalary>> GetSalaryHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<EmployeeSalary> GetCurrentSalaryAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<EmployeeSalary> GetSalaryByIdAsync(Guid salaryId, CancellationToken cancellationToken = default);

        Task<EmployeeSalary> GetSalaryWithLineItemsAsync(Guid salaryId, CancellationToken cancellationToken = default);

        Task<bool> SalaryExistsForDateAsync(Guid employeeId, DateTime effectiveFromDate, CancellationToken cancellationToken = default);

        Task AddSalaryAsync(EmployeeSalary salary, CancellationToken cancellationToken = default);

        Task<EmployeeSalaryComponent> GetSalaryComponentByIdAsync(Guid componentId, CancellationToken cancellationToken = default);

        Task AddSalaryComponentAsync(EmployeeSalaryComponent component, CancellationToken cancellationToken = default);

        void RemoveSalaryComponent(EmployeeSalaryComponent component);

        Task<EmployeeSalaryDeduction> GetSalaryDeductionByIdAsync(Guid deductionId, CancellationToken cancellationToken = default);

        Task AddSalaryDeductionAsync(EmployeeSalaryDeduction deduction, CancellationToken cancellationToken = default);

        void RemoveSalaryDeduction(EmployeeSalaryDeduction deduction);

        Task<EmployeeInsurancePremium> GetInsurancePremiumByIdAsync(Guid premiumId, CancellationToken cancellationToken = default);

        Task AddInsurancePremiumAsync(EmployeeInsurancePremium premium, CancellationToken cancellationToken = default);

        void RemoveInsurancePremium(EmployeeInsurancePremium premium);

        Task<IReadOnlyList<EmployeeLoan>> GetLoansByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<EmployeeLoan> GetLoanByIdAsync(Guid loanId, CancellationToken cancellationToken = default);

        Task AddLoanAsync(EmployeeLoan loan, CancellationToken cancellationToken = default);

        // Payroll-run batch inputs (payroll redesign, 2026-07-16): every payable employee
        // (Active/OnLeave) with their full salary-revision history and line items loaded, and
        // the Approved loans batched across the run's employees.
        Task<IReadOnlyList<Employee>> GetPayrollEligibleEmployeesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EmployeeLoan>> GetApprovedLoansByEmployeeIdsAsync(IReadOnlyList<Guid> employeeIds, CancellationToken cancellationToken = default);

        // SalaryAdjustment (pre-run monthly overrides) is owned by this aggregate, like the
        // salary line items and loans.
        Task<IReadOnlyList<SalaryAdjustment>> GetSalaryAdjustmentsByFilterAsync(Guid? employeeId, Guid? fiscalYearId, int? monthIndex, AdjustmentStatus? status, CancellationToken cancellationToken = default);

        Task<SalaryAdjustment> GetSalaryAdjustmentByIdAsync(Guid adjustmentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalaryAdjustment>> GetPendingSalaryAdjustmentsForPeriodAsync(Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalaryAdjustment>> GetSalaryAdjustmentsAppliedToSlipsAsync(IReadOnlyList<Guid> slipIds, CancellationToken cancellationToken = default);

        Task AddSalaryAdjustmentAsync(SalaryAdjustment adjustment, CancellationToken cancellationToken = default);

        void RemoveSalaryAdjustment(SalaryAdjustment adjustment);

        // Qualifications and Documents (2026-07-23, moved here from ITeacherRepository -- neither
        // concept is teaching-specific, every employee can hold a degree or need an identity
        // document on file).
        Task<IReadOnlyList<EmployeeQualification>> GetQualificationsAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<EmployeeQualification> GetQualificationByIdAsync(Guid qualificationId, CancellationToken cancellationToken = default);

        Task AddQualificationAsync(EmployeeQualification qualification, CancellationToken cancellationToken = default);

        void RemoveQualification(EmployeeQualification qualification);

        Task<IReadOnlyList<EmployeeDocument>> GetDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<EmployeeDocument> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default);

        Task AddDocumentAsync(EmployeeDocument document, CancellationToken cancellationToken = default);

        void RemoveDocument(EmployeeDocument document);

        // Leave balances (2026-07-23) -- owned here like Loans/Adjustments, since a balance is
        // fundamentally an Employee-scoped running total, not its own aggregate root.
        Task<IReadOnlyList<EmployeeLeaveBalance>> GetLeaveBalancesByEmployeeIdAsync(Guid employeeId, Guid fiscalYearId, CancellationToken cancellationToken = default);

        Task<EmployeeLeaveBalance> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, Guid fiscalYearId, CancellationToken cancellationToken = default);

        Task<EmployeeLeaveBalance> GetLeaveBalanceByIdAsync(Guid leaveBalanceId, CancellationToken cancellationToken = default);

        Task AddLeaveBalanceAsync(EmployeeLeaveBalance leaveBalance, CancellationToken cancellationToken = default);

        // TeacherAssignment (2026-08-06, moved here from the removed ITeacherRepository --
        // TeacherId now FKs directly to Employee.Id, so assignments are this aggregate's children
        // like every other Employee child collection).
        Task<bool> HasAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByClassSubjectIdsAsync(IReadOnlyCollection<Guid> classSubjectIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByAcademicClassAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<TeacherAssignment> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

        Task<bool> AssignmentExistsAsync(Guid teacherId, Guid classSubjectId, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<bool> ClassTeacherExistsForSectionAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task<bool> TeacherHasTimePeriodConflictAsync(Guid teacherId, Guid timePeriodId, CancellationToken cancellationToken = default);

        Task AddAssignmentAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default);

        void RemoveAssignment(TeacherAssignment assignment);
    }
}
