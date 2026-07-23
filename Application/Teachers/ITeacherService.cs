using Application.Common.Models;
using Application.Employees.Commands;
using Application.Employees.Dtos;
using Application.Teachers.Commands;
using Application.Teachers.Dtos;
using Application.Teachers.Queries;

namespace Application.Teachers
{
    public interface ITeacherService
    {
        Task<CommonResponse<TeacherDto>> CreateTeacherAsync(CreateTeacherCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherDto>> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<TeacherDto>>> GetTeachersAsync(GetTeachersQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherDto>> UpdateTeacherAsync(Guid id, UpdateTeacherCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteTeacherAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherAssignmentDto>> AssignClassSubjectAsync(Guid teacherId, AssignTeacherCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveAssignmentAsync(Guid teacherId, Guid assignmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TeacherAssignmentDto>>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        // Thin convenience aliases over IEmployeeService's salary machinery -- a Teacher's Id IS
        // its Employee's Id (shared-PK pattern), so these forward directly.
        Task<CommonResponse<EmployeeSalaryDto>> AddSalaryAsync(Guid teacherId, AddEmployeeSalaryCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeSalaryDto>>> GetSalaryHistoryAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeTaxCalculationDto>> GetCurrentSalaryTaxCalculationAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeMonthlyTaxBreakdownDto>> GetMonthlySalaryTaxCalculationAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxPlanningDto>> GetTaxPlanningAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryAnnualForecastDto>> GetAnnualForecastAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxDetailsGridDto>> GetTaxDetailsGridAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentPreviewDto>> GetPayslipPreviewAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<PayslipSummaryDto>>> GetPayslipsAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayslipDetailDto>> GetPayslipDetailAsync(Guid teacherId, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryForecastDto>> GetSalaryForecastAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> RequestLoanAsync(Guid teacherId, RequestLoanCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EmployeeLoanDto>>> GetLoansAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> ApproveLoanAsync(Guid teacherId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> RejectLoanAsync(Guid teacherId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EmployeeLoanDto>> CancelLoanAsync(Guid teacherId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentPreviewDto>> GetIdCardPreviewAsync(Guid teacherId, CancellationToken cancellationToken = default);
    }
}
