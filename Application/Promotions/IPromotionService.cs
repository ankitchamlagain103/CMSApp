using Application.Common.Models;
using Application.Promotions.Commands;
using Application.Promotions.Dtos;

namespace Application.Promotions
{
    public interface IPromotionService
    {
        Task<CommonResponse<StudentPromotionDto>> CreateStudentPromotionAsync(CreateStudentPromotionCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentPromotionDto>> GetStudentPromotionByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<StudentPromotionDto>>> GetStudentPromotionsAsync(Guid? studentId, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<BulkPromotionResultDto>> BulkProcessPromotionAsync(BulkProcessPromotionCommand command, CancellationToken cancellationToken = default);
    }
}
