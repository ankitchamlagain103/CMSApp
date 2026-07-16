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

        [HttpPost("{id:guid}/qualifications")]
        public async Task<ActionResult<CommonResponse<TeacherQualificationDto>>> AddQualification(Guid id, [FromBody] AddTeacherQualificationCommand command, CancellationToken cancellationToken)
        {
            var response = await _teacherService.AddQualificationAsync(id, command, cancellationToken);
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
            var response = await _teacherService.RemoveQualificationAsync(id, qualificationId, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<List<TeacherQualificationDto>>>> GetQualifications(Guid id, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetQualificationsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
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

        // Multipart form-data: the file plus the metadata fields. The service owns every rule
        // (type catalog, extension whitelist, size cap) -- the controller only unwraps IFormFile,
        // since Application can't reference ASP.NET Core types.
        [HttpPost("{id:guid}/documents")]
        public async Task<ActionResult<CommonResponse<TeacherDocumentDto>>> UploadDocument(Guid id, [FromForm] IFormFile file, [FromForm] string documentTypeCode, [FromForm] string documentName, [FromForm] DateTime? validUntil, [FromForm] string remarks, CancellationToken cancellationToken)
        {
            var command = new UploadTeacherDocumentCommand
            {
                DocumentTypeCode = documentTypeCode,
                DocumentName = documentName,
                ValidUntil = validUntil,
                Remarks = remarks
            };

            CommonResponse<TeacherDocumentDto> response;
            if (file == null)
            {
                response = await _teacherService.UploadDocumentAsync(id, command, null, null, null, 0, cancellationToken);
            }
            else
            {
                using var fileStream = file.OpenReadStream();
                response = await _teacherService.UploadDocumentAsync(id, command, fileStream, file.FileName, file.ContentType, file.Length, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<List<TeacherDocumentDto>>>> GetDocuments(Guid id, CancellationToken cancellationToken)
        {
            var response = await _teacherService.GetDocumentsAsync(id, cancellationToken);
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
            var response = await _teacherService.GetDocumentFileAsync(id, documentId, cancellationToken);
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
            var response = await _teacherService.DeleteDocumentAsync(id, documentId, cancellationToken);
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
