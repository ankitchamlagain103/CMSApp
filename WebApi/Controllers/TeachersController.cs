using Application.Common.Models;
using Application.Employees.Commands;
using Application.Employees.Dtos;
using Application.Teachers;
using Application.Teachers.Commands;
using Application.Teachers.Dtos;
using Application.Teachers.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;

        public TeachersController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<TeacherDto>>> CreateTeacher([FromBody] CreateTeacherCommand command, CancellationToken cancellationToken)
        {
            var response = await _teacherService.CreateTeacherAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<TeacherDto>>>> GetTeachers([FromQuery] GetTeachersQuery query, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetTeachersAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<TeacherDto>>> GetTeacherById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetTeacherByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<TeacherDto>>> UpdateTeacher(Guid id, [FromBody] UpdateTeacherCommand command, CancellationToken cancellationToken)
        {
            var response = await _teacherService.UpdateTeacherAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteTeacher(Guid id, CancellationToken cancellationToken)
        {
            var response = await _teacherService.DeleteTeacherAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/assignments")]
        public async Task<ActionResult<CommonResponse<TeacherAssignmentDto>>> AssignClassSubject(Guid id, [FromBody] AssignTeacherCommand command, CancellationToken cancellationToken)
        {
            var response = await _teacherService.AssignClassSubjectAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}/assignments/{assignmentId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveAssignment(Guid id, Guid assignmentId, CancellationToken cancellationToken)
        {
            var response = await _teacherService.RemoveAssignmentAsync(id, assignmentId, cancellationToken);
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

        [HttpGet("{id:guid}/assignments")]
        public async Task<ActionResult<CommonResponse<List<TeacherAssignmentDto>>>> GetAssignments(Guid id, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetAssignmentsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/salaries")]
        public async Task<ActionResult<CommonResponse<EmployeeSalaryDto>>> AddSalary(Guid id, [FromBody] AddEmployeeSalaryCommand command, CancellationToken cancellationToken)
        {
            var response = await _teacherService.AddSalaryAsync(id, command, cancellationToken);
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
            var response = await _teacherService.GetSalaryHistoryAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/salaries/tax-calculation")]
        public async Task<ActionResult<CommonResponse<EmployeeTaxCalculationDto>>> GetSalaryTaxCalculation(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetCurrentSalaryTaxCalculationAsync(id, fiscalYearId, cancellationToken);
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
            var response = await _teacherService.GetPayslipPreviewAsync(id, fiscalYearId, cancellationToken);
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
            var response = await _teacherService.GetMonthlySalaryTaxCalculationAsync(id, fiscalYearId, cancellationToken);
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
            var response = await _teacherService.GetTaxPlanningAsync(id, fiscalYearId, cancellationToken);
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

        [HttpGet("{id:guid}/salaries/annual-forecast")]
        public async Task<ActionResult<CommonResponse<SalaryAnnualForecastDto>>> GetAnnualForecast(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetAnnualForecastAsync(id, fiscalYearId, cancellationToken);
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

        [HttpGet("{id:guid}/salaries/tax-details")]
        public async Task<ActionResult<CommonResponse<TaxDetailsGridDto>>> GetTaxDetailsGrid(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetTaxDetailsGridAsync(id, fiscalYearId, cancellationToken);
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
            var response = await _teacherService.GetPayslipsAsync(id, fiscalYearId, cancellationToken);
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
            var response = await _teacherService.GetPayslipDetailAsync(id, fiscalYearId, monthIndex, cancellationToken);
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
            var response = await _teacherService.GetSalaryForecastAsync(id, fiscalYearId, cancellationToken);
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
            var response = await _teacherService.RequestLoanAsync(id, command, cancellationToken);
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
            var response = await _teacherService.GetLoansAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/loans/{loanId:guid}/approve")]
        public async Task<ActionResult<CommonResponse<EmployeeLoanDto>>> ApproveLoan(Guid id, Guid loanId, [FromBody] LoanRemarksCommand command, CancellationToken cancellationToken)
        {
            var response = await _teacherService.ApproveLoanAsync(id, loanId, command, cancellationToken);
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
            var response = await _teacherService.RejectLoanAsync(id, loanId, command, cancellationToken);
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
            var response = await _teacherService.CancelLoanAsync(id, loanId, command, cancellationToken);
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

        [HttpGet("{id:guid}/id-card-preview")]
        public async Task<ActionResult<CommonResponse<DocumentPreviewDto>>> GetIdCardPreview(Guid id, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetIdCardPreviewAsync(id, cancellationToken);
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
