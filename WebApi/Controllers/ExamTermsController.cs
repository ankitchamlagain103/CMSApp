using Application.Common.Models;
using Application.Exams;
using Application.Exams.Commands;
using Application.Exams.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamTermsController : ControllerBase
    {
        private readonly IExamService _examService;

        public ExamTermsController(IExamService examService)
        {
            _examService = examService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<ExamTermDto>>> CreateExamTerm([FromBody] CreateExamTermCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.CreateExamTermAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<ExamTermDto>>>> GetExamTerms([FromQuery] Guid? academicYearId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _examService.GetExamTermsAsync(academicYearId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<ExamTermDto>>> GetExamTermById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.GetExamTermByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<ExamTermDto>>> UpdateExamTerm(Guid id, [FromBody] UpdateExamTermCommand command, CancellationToken cancellationToken)
        {
            var response = await _examService.UpdateExamTermAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteExamTerm(Guid id, CancellationToken cancellationToken)
        {
            var response = await _examService.DeleteExamTermAsync(id, cancellationToken);
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
