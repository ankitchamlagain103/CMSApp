using Application.Common.Models;
using Application.Exams;
using Application.Exams.Commands;
using Application.Exams.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamResultsController : ControllerBase
    {
        private readonly IExamService _examService;

        public ExamResultsController(IExamService examService)
        {
            _examService = examService;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<CommonResponse<GenerateExamResultsResultDto>>> GenerateExamResults([FromBody] GenerateExamResultsCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.GenerateExamResultsAsync(command, cancellationToken);
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

        [HttpPost("publish/{examTermId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> PublishExamResults(Guid examTermId, CancellationToken cancellationToken)
        {
            var response = await _examService.PublishExamResultsAsync(examTermId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<StudentResultDto>>>> GetStudentResults([FromQuery] Guid? examTermId, [FromQuery] Guid? classSectionId, [FromQuery] Guid? enrollmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _examService.GetStudentResultsAsync(examTermId, classSectionId, enrollmentId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentResultDetailDto>>> GetStudentResultById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.GetStudentResultByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/withhold")]
        public async Task<ActionResult<CommonResponse<StudentResultDto>>> WithholdExamResult(Guid id, [FromBody] WithholdExamResultCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.WithholdExamResultAsync(id, command, cancellationToken);
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

        [HttpPost("{id:guid}/lift-withhold")]
        public async Task<ActionResult<CommonResponse<bool>>> LiftExamResultWithhold(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.LiftExamResultWithholdAsync(id, cancellationToken);
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
