using Application.Common.Models;
using Application.Students;
using Application.Students.Commands;
using Application.Students.Dtos;
using Application.Students.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<StudentDto>>> CreateStudent([FromBody] CreateStudentCommand command, CancellationToken cancellationToken)
        {
            var response = await _studentService.CreateStudentAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<StudentDto>>>> GetStudents([FromQuery] GetStudentsQuery query, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetStudentsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentDto>>> GetStudentById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetStudentByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // Self-service (2026-08-07) -- "a teacher should see his or her students details only".
        // No {id} route parameter for the list: the caller's own Employee is resolved from the
        // JWT and their students scoped to the ClassSectionIds they're actually assigned to teach.
        // DefaultEnabledMenu-gated, no permission row -- same "self access to your own scope isn't
        // a privilege" reasoning as every other "Me" route in this codebase.

        [HttpGet("me")]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<StudentDto>>>> GetMyStudents([FromQuery] GetStudentsQuery query, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetMyStudentsAsync(query, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("me/{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentDto>>> GetMyStudentById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetMyStudentByIdAsync(id, cancellationToken);
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

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentDto>>> UpdateStudent(Guid id, [FromBody] UpdateStudentCommand command, CancellationToken cancellationToken)
        {
            var response = await _studentService.UpdateStudentAsync(id, command, cancellationToken);
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

        // Portal account provisioning retrofit (2026-07-27) -- for a student that didn't get a
        // login at creation time (CreateStudentCommand.RegisterUserAccount is the create-time
        // path). No body -- always the fixed RoleNames.Student role.
        [HttpPost("{id:guid}/register-account")]
        public async Task<ActionResult<CommonResponse<StudentDto>>> RegisterUserAccount(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.RegisterUserAccountAsync(id, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteStudent(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.DeleteStudentAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/guardians")]
        public async Task<ActionResult<CommonResponse<StudentGuardianDto>>> LinkGuardian(Guid id, [FromBody] LinkGuardianCommand command, CancellationToken cancellationToken)
        {
            var response = await _studentService.LinkGuardianAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}/guardians/{linkId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> UnlinkGuardian(Guid id, Guid linkId, CancellationToken cancellationToken)
        {
            var response = await _studentService.UnlinkGuardianAsync(id, linkId, cancellationToken);
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

        [HttpGet("{id:guid}/guardians")]
        public async Task<ActionResult<CommonResponse<List<StudentGuardianDto>>>> GetGuardians(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetGuardiansAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // Multipart form-data, mirroring the teacher documents endpoint: the file plus metadata
        // fields; every rule (type catalog, extension whitelist, size cap) lives in the service.
        [HttpPost("{id:guid}/documents")]
        public async Task<ActionResult<CommonResponse<StudentDocumentDto>>> UploadDocument(Guid id, [FromForm] IFormFile file, [FromForm] string documentTypeCode, [FromForm] string documentName, [FromForm] DateTime? validUntil, [FromForm] string remarks, CancellationToken cancellationToken)
        {
            var command = new UploadStudentDocumentCommand
            {
                DocumentTypeCode = documentTypeCode,
                DocumentName = documentName,
                ValidUntil = validUntil,
                Remarks = remarks
            };

            CommonResponse<StudentDocumentDto> response;
            if (file == null)
            {
                response = await _studentService.UploadDocumentAsync(id, command, null, null, null, 0, cancellationToken);
            }
            else
            {
                using var fileStream = file.OpenReadStream();
                response = await _studentService.UploadDocumentAsync(id, command, fileStream, file.FileName, file.ContentType, file.Length, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<List<StudentDocumentDto>>>> GetDocuments(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetDocumentsAsync(id, cancellationToken);
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
            var response = await _studentService.GetDocumentFileAsync(id, documentId, cancellationToken);
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
            var response = await _studentService.DeleteDocumentAsync(id, documentId, cancellationToken);
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

        [HttpGet("{id:guid}/enrollment-history")]
        public async Task<ActionResult<CommonResponse<List<StudentEnrollmentHistoryDto>>>> GetEnrollmentHistory(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetEnrollmentHistoryAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/timetable")]
        public async Task<ActionResult<CommonResponse<StudentTimetableDto>>> GetTimetable(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetTimetableAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/id-card-preview")]
        public async Task<ActionResult<CommonResponse<DocumentPreviewDto>>> GetIdCardPreview(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetIdCardPreviewAsync(id, cancellationToken);
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
