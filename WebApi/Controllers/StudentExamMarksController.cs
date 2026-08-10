using Application.Common.Models;
using Application.Exams;
using Application.Exams.Commands;
using Application.Exams.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentExamMarksController : ControllerBase
    {
        private readonly IExamService _examService;

        public StudentExamMarksController(IExamService examService)
        {
            _examService = examService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<StudentExamMarkDto>>> CreateStudentExamMark([FromBody] CreateStudentExamMarkCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.CreateStudentExamMarkAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode == ResponseCodes.Conflict)
            {
                return Conflict(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<List<StudentExamMarkDto>>>> GetStudentExamMarks([FromQuery] Guid? examId, [FromQuery] Guid? enrollmentId, [FromQuery] Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            var response = await _examService.GetStudentExamMarksAsync(examId, enrollmentId, classSectionId, cancellationToken);
            return Ok(response);
        }

        // Self-service marks entry (2026-08-07) -- "show them only the subject he or she teaches
        // for marks entry": every action below resolves the caller's own Employee from the JWT and
        // 403s if the target exam's subject isn't one of their own TeacherAssignment rows. examId
        // is required here (unlike the admin GetStudentExamMarks above) since it's what the check
        // runs against.

        [HttpPost("me")]
        public async Task<ActionResult<CommonResponse<StudentExamMarkDto>>> CreateMyStudentExamMark([FromBody] CreateStudentExamMarkCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.CreateMyStudentExamMarkAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode == ResponseCodes.Conflict)
            {
                return Conflict(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("me")]
        public async Task<ActionResult<CommonResponse<List<StudentExamMarkDto>>>> GetMyStudentExamMarks([FromQuery] Guid examId, [FromQuery] Guid? enrollmentId, [FromQuery] Guid? classSectionId, CancellationToken cancellationToken)
        {
            var response = await _examService.GetMyStudentExamMarksAsync(examId, enrollmentId, classSectionId, cancellationToken);
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

        [HttpPut("me/{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentExamMarkDto>>> UpdateMyStudentExamMark(Guid id, [FromBody] UpdateStudentExamMarkCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.UpdateMyStudentExamMarkAsync(id, command, cancellationToken);
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

        [HttpGet("me/roster")]
        public async Task<ActionResult<CommonResponse<List<ExamMarkRosterItemDto>>>> GetMyStudentExamMarkRoster([FromQuery] Guid examId, [FromQuery] string search, [FromQuery] Guid? classSectionId, CancellationToken cancellationToken)
        {
            var response = await _examService.GetMyStudentExamMarkRosterAsync(examId, search, classSectionId, cancellationToken);
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

        [HttpPost("me/bulk")]
        public async Task<ActionResult<CommonResponse<BulkUpsertStudentExamMarksResultDto>>> BulkUpsertMyStudentExamMarks([FromBody] BulkUpsertStudentExamMarksCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.BulkUpsertMyStudentExamMarksAsync(command, cancellationToken);
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

        // Admin, student-wise marks entry: every subject/exam within one term the given
        // enrollment is eligible for, each carrying its existing mark (or null) -- pick a student,
        // enter every subject's marks in one screen. Complements the roster below, which is the
        // teacher-wise "one subject, every student" flow.
        [HttpGet("student/{enrollmentId:guid}")]
        public async Task<ActionResult<CommonResponse<List<StudentExamMarkByStudentItemDto>>>> GetStudentExamMarksByStudent(Guid enrollmentId, [FromQuery] Guid examTermId, CancellationToken cancellationToken)
        {
            var response = await _examService.GetStudentExamMarksByStudentAsync(enrollmentId, examTermId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentExamMarkDto>>> GetStudentExamMarkById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.GetStudentExamMarkByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentExamMarkDto>>> UpdateStudentExamMark(Guid id, [FromBody] UpdateStudentExamMarkCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.UpdateStudentExamMarkAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteStudentExamMark(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.DeleteStudentExamMarkAsync(id, cancellationToken);
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

        // Search-and-select worklist: every enrolled student eligible for the exam's subject, each
        // row carrying its existing mark (or null) -- the "search a student, then enter their
        // marks" flow. classSectionId (optional) narrows to one section -- pass the calling
        // teacher's own assigned section for this subject so a teacher-wise entry screen only
        // ever shows their own section's students, even though the Exam itself spans the grade.
        [HttpGet("roster")]
        public async Task<ActionResult<CommonResponse<List<ExamMarkRosterItemDto>>>> GetStudentExamMarkRoster([FromQuery] Guid examId, [FromQuery] string search, [FromQuery] Guid? classSectionId, CancellationToken cancellationToken)
        {
            var response = await _examService.GetStudentExamMarkRosterAsync(examId, search, classSectionId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<CommonResponse<BulkUpsertStudentExamMarksResultDto>>> BulkUpsertStudentExamMarks([FromBody] BulkUpsertStudentExamMarksCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.BulkUpsertStudentExamMarksAsync(command, cancellationToken);
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
