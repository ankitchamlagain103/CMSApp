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

        Task<Employee> GetByIdWithTeacherAsync(Guid id, CancellationToken cancellationToken = default);

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
    }
}
