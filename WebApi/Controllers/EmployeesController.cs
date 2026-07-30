using Application.Common.Models;
using Application.Employees;
using Application.Employees.Commands;
using Application.Employees.Dtos;
using Application.Employees.Queries;
using Application.Notifications;
using Application.Notifications.Dtos;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly INotificationService _notificationService;

        public EmployeesController(IEmployeeService employeeService, INotificationService notificationService)
        {
            _employeeService = employeeService;
            _notificationService = notificationService;
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

        // Portal account provisioning retrofit (2026-07-27) -- for an employee that didn't get a
        // login at creation time (CreateEmployeeCommand.RegisterUserAccount is the create-time path).
        [HttpPost("{id:guid}/register-account")]
        public async Task<ActionResult<CommonResponse<EmployeeDto>>> RegisterUserAccount(Guid id, [FromBody] RegisterEmployeeUserAccountCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RegisterUserAccountAsync(id, command, cancellationToken);
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

        // 2026-07-23: Qualifications and Documents, generic to every employee (moved off
        // Teacher entirely -- neither concept was ever teaching-specific).

        [HttpPost("{id:guid}/qualifications")]
        public async Task<ActionResult<CommonResponse<EmployeeQualificationDto>>> AddQualification(Guid id, [FromBody] AddEmployeeQualificationCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AddQualificationAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}/qualifications/{qualificationId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveQualification(Guid id, Guid qualificationId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RemoveQualificationAsync(id, qualificationId, cancellationToken);
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

        [HttpGet("{id:guid}/qualifications")]
        public async Task<ActionResult<CommonResponse<List<EmployeeQualificationDto>>>> GetQualifications(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetQualificationsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // Multipart form-data: the file plus the metadata fields. The service owns every rule
        // (type catalog, extension whitelist, size cap) -- the controller only unwraps IFormFile,
        // since Application can't reference ASP.NET Core types.
        [HttpPost("{id:guid}/documents")]
        public async Task<ActionResult<CommonResponse<EmployeeDocumentDto>>> UploadDocument(Guid id, [FromForm] IFormFile file, [FromForm] string documentTypeCode, [FromForm] string documentName, [FromForm] DateTime? validUntil, [FromForm] string remarks, CancellationToken cancellationToken)
        {
            var command = new UploadEmployeeDocumentCommand
            {
                DocumentTypeCode = documentTypeCode,
                DocumentName = documentName,
                ValidUntil = validUntil,
                Remarks = remarks
            };

            CommonResponse<EmployeeDocumentDto> response;
            if (file == null)
            {
                response = await _employeeService.UploadDocumentAsync(id, command, null, null, null, 0, cancellationToken);
            }
            else
            {
                using var fileStream = file.OpenReadStream();
                response = await _employeeService.UploadDocumentAsync(id, command, fileStream, file.FileName, file.ContentType, file.Length, cancellationToken);
            }

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

        [HttpGet("{id:guid}/documents")]
        public async Task<ActionResult<CommonResponse<List<EmployeeDocumentDto>>>> GetDocuments(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetDocumentsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // Streams the raw file (no envelope) -- errors still come back enveloped.
        [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
        public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetDocumentFileAsync(id, documentId, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return NotFound(response);
            }

            var fileResult = File(response.Data.Content, response.Data.ContentType, response.Data.FileName);
            return fileResult;
        }

        [HttpDelete("{id:guid}/documents/{documentId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.DeleteDocumentAsync(id, documentId, cancellationToken);
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

        [HttpGet("{id:guid}/salaries/annual-forecast")]
        public async Task<ActionResult<CommonResponse<SalaryAnnualForecastDto>>> GetAnnualForecast(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetAnnualForecastAsync(id, fiscalYearId, cancellationToken);
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
            var response = await _employeeService.GetTaxDetailsGridAsync(id, fiscalYearId, cancellationToken);
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

        [HttpPost("{id:guid}/salaries/{salaryId:guid}/lines")]
        public async Task<ActionResult<CommonResponse<SalaryLineDto>>> AddSalaryLine(Guid id, Guid salaryId, [FromBody] SalaryLineInput command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AddSalaryLineAsync(id, salaryId, command, cancellationToken);
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

        [HttpDelete("{id:guid}/salaries/{salaryId:guid}/lines/{lineId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveSalaryLine(Guid id, Guid salaryId, Guid lineId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RemoveSalaryLineAsync(id, salaryId, lineId, cancellationToken);
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

        // 2026-07-23: profile photo.

        [HttpPost("{id:guid}/photo")]
        public async Task<ActionResult<CommonResponse<bool>>> UploadPhoto(Guid id, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            CommonResponse<bool> response;
            if (file == null)
            {
                response = await _employeeService.UploadPhotoAsync(id, null, null, null, 0, cancellationToken);
            }
            else
            {
                using var fileStream = file.OpenReadStream();
                response = await _employeeService.UploadPhotoAsync(id, fileStream, file.FileName, file.ContentType, file.Length, cancellationToken);
            }

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

        [HttpGet("{id:guid}/photo/download")]
        public async Task<IActionResult> DownloadPhoto(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetPhotoFileAsync(id, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return NotFound(response);
            }

            var fileResult = File(response.Data.Content, response.Data.ContentType, response.Data.FileName);
            return fileResult;
        }

        [HttpDelete("{id:guid}/photo")]
        public async Task<ActionResult<CommonResponse<bool>>> DeletePhoto(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.DeletePhotoAsync(id, cancellationToken);
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

        // 2026-07-23: leave balances.

        [HttpPost("{id:guid}/leavebalances")]
        public async Task<ActionResult<CommonResponse<EmployeeLeaveBalanceDto>>> AllocateLeaveBalance(Guid id, [FromBody] AllocateLeaveBalanceCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AllocateLeaveBalanceAsync(id, command, cancellationToken);
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

        [HttpGet("{id:guid}/leavebalances")]
        public async Task<ActionResult<CommonResponse<List<EmployeeLeaveBalanceDto>>>> GetLeaveBalances(Guid id, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetLeaveBalancesAsync(id, fiscalYearId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // 2026-07-23: leave requests. Create is multipart/form-data (optional attachment file),
        // same [FromForm] unwrap-only-at-the-controller pattern as document upload.

        [HttpPost("{id:guid}/leaverequests")]
        public async Task<ActionResult<CommonResponse<LeaveRequestDto>>> CreateLeaveRequest(Guid id, [FromForm] Guid leaveTypeId, [FromForm] DateTime fromDate, [FromForm] DateTime toDate, [FromForm] string reason, [FromForm] Guid? substituteEmployeeId, [FromForm] bool isEmergency, [FromForm] IFormFile attachment, CancellationToken cancellationToken)
        {
            var command = new CreateLeaveRequestCommand
            {
                LeaveTypeId = leaveTypeId,
                FromDate = fromDate,
                ToDate = toDate,
                Reason = reason,
                SubstituteEmployeeId = substituteEmployeeId,
                IsEmergency = isEmergency
            };

            CommonResponse<LeaveRequestDto> response;
            if (attachment == null)
            {
                response = await _employeeService.CreateLeaveRequestAsync(id, command, null, null, null, 0, cancellationToken);
            }
            else
            {
                using var attachmentStream = attachment.OpenReadStream();
                response = await _employeeService.CreateLeaveRequestAsync(id, command, attachmentStream, attachment.FileName, attachment.ContentType, attachment.Length, cancellationToken);
            }

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

        [HttpGet("{id:guid}/leaverequests")]
        public async Task<ActionResult<CommonResponse<List<LeaveRequestDto>>>> GetLeaveRequests(Guid id, [FromQuery] LeaveApprovalStatus? managerStatus, [FromQuery] LeaveApprovalStatus? hrStatus, [FromQuery] bool? isPending, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetLeaveRequestsAsync(id, managerStatus, hrStatus, isPending, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/leaverequests/{requestId:guid}")]
        public async Task<ActionResult<CommonResponse<LeaveRequestDto>>> GetLeaveRequestById(Guid id, Guid requestId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetLeaveRequestByIdAsync(id, requestId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/leaverequests/{requestId:guid}/manager-approve")]
        public async Task<ActionResult<CommonResponse<LeaveRequestDto>>> ApproveManagerDecision(Guid id, Guid requestId, [FromBody] LeaveDecisionCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.ApproveManagerDecisionAsync(id, requestId, command, cancellationToken);
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

        [HttpPost("{id:guid}/leaverequests/{requestId:guid}/manager-reject")]
        public async Task<ActionResult<CommonResponse<LeaveRequestDto>>> RejectManagerDecision(Guid id, Guid requestId, [FromBody] LeaveDecisionCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RejectManagerDecisionAsync(id, requestId, command, cancellationToken);
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

        // HR decisions are deliberately not gated on ManagerStatus -- see
        // IEmployeeService.ApproveHrDecisionAsync's doc comment (the "emergency override" case).
        [HttpPost("{id:guid}/leaverequests/{requestId:guid}/hr-approve")]
        public async Task<ActionResult<CommonResponse<LeaveRequestDto>>> ApproveHrDecision(Guid id, Guid requestId, [FromBody] LeaveDecisionCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.ApproveHrDecisionAsync(id, requestId, command, cancellationToken);
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

        [HttpPost("{id:guid}/leaverequests/{requestId:guid}/hr-reject")]
        public async Task<ActionResult<CommonResponse<LeaveRequestDto>>> RejectHrDecision(Guid id, Guid requestId, [FromBody] LeaveDecisionCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RejectHrDecisionAsync(id, requestId, command, cancellationToken);
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

        [HttpPost("{id:guid}/leaverequests/{requestId:guid}/cancel")]
        public async Task<ActionResult<CommonResponse<bool>>> CancelLeaveRequest(Guid id, Guid requestId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.CancelLeaveRequestAsync(id, requestId, cancellationToken);
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

        [HttpPost("{id:guid}/leaverequests/{requestId:guid}/substitutes")]
        public async Task<ActionResult<CommonResponse<LeaveSubstituteDto>>> AddLeaveSubstitute(Guid id, Guid requestId, [FromBody] AddLeaveSubstituteCommand command, CancellationToken cancellationToken)
        {
            var response = await _employeeService.AddLeaveSubstituteAsync(id, requestId, command, cancellationToken);
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

        [HttpDelete("{id:guid}/leaverequests/{requestId:guid}/substitutes/{substituteId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveLeaveSubstitute(Guid id, Guid requestId, Guid substituteId, CancellationToken cancellationToken)
        {
            var response = await _employeeService.RemoveLeaveSubstituteAsync(id, requestId, substituteId, cancellationToken);
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

        // 2026-07-23: notifications. No employee-login exists yet, so this is the admin-facing
        // "what's been sent to this employee" view, same convention as every other
        // Employee-scoped sub-resource in this codebase -- not a self-service inbox.

        [HttpGet("{id:guid}/notifications")]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<NotificationDto>>>> GetNotifications(Guid id, [FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _notificationService.GetNotificationsAsync(id, isRead, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:guid}/notifications/{notificationId:guid}/read")]
        public async Task<ActionResult<CommonResponse<bool>>> MarkNotificationRead(Guid id, Guid notificationId, CancellationToken cancellationToken)
        {
            var response = await _notificationService.MarkReadAsync(id, notificationId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/notifications/read-all")]
        public async Task<ActionResult<CommonResponse<bool>>> MarkAllNotificationsRead(Guid id, CancellationToken cancellationToken)
        {
            var response = await _notificationService.MarkAllReadAsync(id, cancellationToken);
            return Ok(response);
        }

        // 2026-07-23: composite Employee Profile page.
        [HttpGet("{id:guid}/profile")]
        public async Task<ActionResult<CommonResponse<EmployeeProfileDto>>> GetEmployeeProfile(Guid id, CancellationToken cancellationToken)
        {
            var response = await _employeeService.GetEmployeeProfileAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
