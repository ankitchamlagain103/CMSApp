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

        Task<CommonResponse<TeacherProfileDto>> PromoteToTeacherAsync(Guid employeeId, PromoteToTeacherCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeSalaryDto>> AddSalaryAsync(Guid employeeId, AddEmployeeSalaryCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeSalaryDto>>> GetSalaryHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeTaxCalculationDto>> GetCurrentSalaryTaxCalculationAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeMonthlyTaxBreakdownDto>> GetMonthlySalaryTaxCalculationAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxPlanningDto>> GetTaxPlanningAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryComponentDto>> AddSalaryComponentAsync(Guid employeeId, Guid salaryId, SalaryComponentInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSalaryComponentAsync(Guid employeeId, Guid salaryId, Guid componentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryDeductionDto>> AddSalaryDeductionAsync(Guid employeeId, Guid salaryId, SalaryDeductionInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSalaryDeductionAsync(Guid employeeId, Guid salaryId, Guid deductionId, CancellationToken cancellationToken = default);

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
    }
}
