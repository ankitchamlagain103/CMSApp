using Application.Common.Models;
using Application.Promotions;
using Application.Promotions.Commands;
using Application.Promotions.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentPromotionsController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public StudentPromotionsController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<StudentPromotionDto>>> CreateStudentPromotion([FromBody] CreateStudentPromotionCommand command, CancellationToken cancellationToken)
        {
            var response = await _promotionService.CreateStudentPromotionAsync(command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<PaginatedResponse<StudentPromotionDto>>>> GetStudentPromotions([FromQuery] Guid? studentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _promotionService.GetStudentPromotionsAsync(studentId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentPromotionDto>>> GetStudentPromotionById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _promotionService.GetStudentPromotionByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("bulk-process")]
        public async Task<ActionResult<CommonResponse<BulkPromotionResultDto>>> BulkProcessPromotion([FromBody] BulkProcessPromotionCommand command, CancellationToken cancellationToken)
        {
            var response = await _promotionService.BulkProcessPromotionAsync(command, cancellationToken);
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
