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
        public async Task<ActionResult<CommonResponse<List<StudentExamMarkDto>>>> GetStudentExamMarks([FromQuery] Guid? examId, [FromQuery] Guid? enrollmentId, CancellationToken cancellationToken = default)
        {
            var response = await _examService.GetStudentExamMarksAsync(examId, enrollmentId, cancellationToken);
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

        // Search-and-select worklist: every enrolled student in the exam's section, each row
        // carrying its existing mark (or null) -- the "search a student, then enter their marks"
        // flow.
        [HttpGet("roster")]
        public async Task<ActionResult<CommonResponse<List<ExamMarkRosterItemDto>>>> GetStudentExamMarkRoster([FromQuery] Guid examId, [FromQuery] string search, CancellationToken cancellationToken)
        {
            var response = await _examService.GetStudentExamMarkRosterAsync(examId, search, cancellationToken);
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
