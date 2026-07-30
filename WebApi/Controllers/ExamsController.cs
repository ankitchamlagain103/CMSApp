using Application.Common.Models;
using Application.Exams;
using Application.Exams.Commands;
using Application.Exams.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    // One Exam per (ExamTerm, ClassSubject) -- always covers the whole grade, never a single
    // section (see the Exam entity's doc comment). Full Marks/Pass Marks are never accepted
    // here; they're read from the linked ClassSubject (configure via AcademicClasses' subject
    // endpoints).
    [ApiController]
    [Route("api/[controller]")]
    public class ExamsController : ControllerBase
    {
        private readonly IExamService _examService;

        public ExamsController(IExamService examService)
        {
            _examService = examService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<ExamDto>>> CreateExam([FromBody] CreateExamCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.CreateExamAsync(command, cancellationToken);
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

        // Schedules an Exam for every listed subject of one class within one exam term, in one
        // call -- the "set the whole routine at once" flow. Always 200 on a found term/class (per-
        // item failures live in the result's Skipped list, same convention as other bulk endpoints
        // like BulkUpsertStudentExamMarks/GenerateExamResults).
        [HttpPost("routine")]
        public async Task<ActionResult<CommonResponse<CreateExamRoutineResultDto>>> CreateExamRoutine([FromBody] CreateExamRoutineCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.CreateExamRoutineAsync(command, cancellationToken);
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

        [HttpGet]
        public async Task<ActionResult<CommonResponse<List<ExamDto>>>> GetExams([FromQuery] Guid? examTermId, [FromQuery] Guid? classSubjectId, [FromQuery] Guid? teacherId, CancellationToken cancellationToken = default)
        {
            var response = await _examService.GetExamsAsync(examTermId, classSubjectId, teacherId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<ExamDto>>> GetExamById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.GetExamByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<ExamDto>>> UpdateExam(Guid id, [FromBody] UpdateExamCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.UpdateExamAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteExam(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.DeleteExamAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/lock")]
        public async Task<ActionResult<CommonResponse<ExamDto>>> LockExam(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.LockExamAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/unlock")]
        public async Task<ActionResult<CommonResponse<ExamDto>>> UnlockExam(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.UnlockExamAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
