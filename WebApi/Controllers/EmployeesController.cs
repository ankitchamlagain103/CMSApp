using Application.Common.Models;
using Application.Employees;
using Application.Employees.Commands;
using Application.Employees.Dtos;
using Application.Employees.Queries;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<EmployeeDto>>> CreateEmployee([FromBody] CreateEmployeeCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.CreateEmployeeAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<EmployeeDto>>>> GetEmployees([FromQuery] GetEmployeesQuery query, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetEmployeesAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<EmployeeDto>>> GetEmployeeById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetEmployeeByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<EmployeeDto>>> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.UpdateEmployeeAsync(id, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteEmployee(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.DeleteEmployeeAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/teacher-profile")]
        public async Task<ActionResult<CommonResponse<TeacherProfileDto>>> PromoteToTeacher(Guid id, [FromBody] PromoteToTeacherCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.PromoteToTeacherAsync(id, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/salaries")]
        public async Task<ActionResult<CommonResponse<EmployeeSalaryDto>>> AddSalary(Guid id, [FromBody] AddEmployeeSalaryCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AddSalaryAsync(id, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/salaries")]
        public async Task<ActionResult<CommonResponse<List<EmployeeSalaryDto>>>> GetSalaryHistory(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetSalaryHistoryAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/salaries/tax-calculation")]
        public async Task<ActionResult<CommonResponse<EmployeeTaxCalculationDto>>> GetSalaryTaxCalculation(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetCurrentSalaryTaxCalculationAsync(id, fiscalYearId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/salaries/payslip-preview")]
        public async Task<ActionResult<CommonResponse<DocumentPreviewDto>>> GetPayslipPreview(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetPayslipPreviewAsync(id, fiscalYearId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/salaries/tax-calculation/monthly")]
        public async Task<ActionResult<CommonResponse<EmployeeMonthlyTaxBreakdownDto>>> GetMonthlySalaryTaxCalculation(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetMonthlySalaryTaxCalculationAsync(id, fiscalYearId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/salaries/tax-planning")]
        public async Task<ActionResult<CommonResponse<TaxPlanningDto>>> GetTaxPlanning(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetTaxPlanningAsync(id, fiscalYearId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/payslips")]
        public async Task<ActionResult<CommonResponse<List<PayslipSummaryDto>>>> GetPayslips(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetPayslipsAsync(id, fiscalYearId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/payslips/{fiscalYearId:guid}/{monthIndex:int}")]
        public async Task<ActionResult<CommonResponse<PayslipDetailDto>>> GetPayslipDetail(Guid id, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetPayslipDetailAsync(id, fiscalYearId, monthIndex, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/salary-forecast")]
        public async Task<ActionResult<CommonResponse<SalaryForecastDto>>> GetSalaryForecast(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetSalaryForecastAsync(id, fiscalYearId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/loans")]
        public async Task<ActionResult<CommonResponse<EmployeeLoanDto>>> RequestLoan(Guid id, [FromBody] RequestLoanCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RequestLoanAsync(id, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/loans")]
        public async Task<ActionResult<CommonResponse<List<EmployeeLoanDto>>>> GetLoans(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetLoansAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/loans/{loanId:guid}/approve")]
        public async Task<ActionResult<CommonResponse<EmployeeLoanDto>>> ApproveLoan(Guid id, Guid loanId, [FromBody] LoanRemarksCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.ApproveLoanAsync(id, loanId, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/loans/{loanId:guid}/reject")]
        public async Task<ActionResult<CommonResponse<EmployeeLoanDto>>> RejectLoan(Guid id, Guid loanId, [FromBody] LoanRemarksCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RejectLoanAsync(id, loanId, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/loans/{loanId:guid}/cancel")]
        public async Task<ActionResult<CommonResponse<EmployeeLoanDto>>> CancelLoan(Guid id, Guid loanId, [FromBody] LoanRemarksCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.CancelLoanAsync(id, loanId, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/salaries/{salaryId:guid}/components")]
        public async Task<ActionResult<CommonResponse<SalaryComponentDto>>> AddSalaryComponent(Guid id, Guid salaryId, [FromBody] SalaryComponentInput command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AddSalaryComponentAsync(id, salaryId, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}/salaries/{salaryId:guid}/components/{componentId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveSalaryComponent(Guid id, Guid salaryId, Guid componentId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RemoveSalaryComponentAsync(id, salaryId, componentId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/salaries/{salaryId:guid}/deductions")]
        public async Task<ActionResult<CommonResponse<SalaryDeductionDto>>> AddSalaryDeduction(Guid id, Guid salaryId, [FromBody] SalaryDeductionInput command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AddSalaryDeductionAsync(id, salaryId, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}/salaries/{salaryId:guid}/deductions/{deductionId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveSalaryDeduction(Guid id, Guid salaryId, Guid deductionId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RemoveSalaryDeductionAsync(id, salaryId, deductionId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/salaries/{salaryId:guid}/insurance-premiums")]
        public async Task<ActionResult<CommonResponse<InsurancePremiumDto>>> AddInsurancePremium(Guid id, Guid salaryId, [FromBody] InsurancePremiumInput command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AddInsurancePremiumAsync(id, salaryId, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}/salaries/{salaryId:guid}/insurance-premiums/{premiumId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveInsurancePremium(Guid id, Guid salaryId, Guid premiumId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RemoveInsurancePremiumAsync(id, salaryId, premiumId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/adjustments")]
        public async Task<ActionResult<CommonResponse<List<SalaryAdjustmentDto>>>> GetSalaryAdjustments(Guid id, [FromQuery] Guid? fiscalYearId, [FromQuery] int? monthIndex, [FromQuery] AdjustmentStatus? status, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetSalaryAdjustmentsAsync(id, fiscalYearId, monthIndex, status, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("adjustments/bulk")]
        public async Task<ActionResult<CommonResponse<BulkSalaryAdjustmentResultDto>>> CreateBulkSalaryAdjustments([FromBody] CreateBulkSalaryAdjustmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.CreateBulkSalaryAdjustmentsAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/adjustments")]
        public async Task<ActionResult<CommonResponse<SalaryAdjustmentDto>>> CreateSalaryAdjustment(Guid id, [FromBody] CreateSalaryAdjustmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.CreateSalaryAdjustmentAsync(id, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}/adjustments/{adjustmentId:guid}")]
        public async Task<ActionResult<CommonResponse<SalaryAdjustmentDto>>> UpdateSalaryAdjustment(Guid id, Guid adjustmentId, [FromBody] UpdateSalaryAdjustmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.UpdateSalaryAdjustmentAsync(id, adjustmentId, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}/adjustments/{adjustmentId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> CancelSalaryAdjustment(Guid id, Guid adjustmentId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.CancelSalaryAdjustmentAsync(id, adjustmentId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
