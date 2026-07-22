using Application.Common.Interfaces;
using Application.Common.Models;
using Application.FeeGenerationRuns.Dtos;
using Application.FeeGenerationRuns.Queries;
using Application.FeeInvoices;
using Application.FeeInvoices.Commands;
using Application.FeeInvoices.Dtos;
using Domain.Common.Filters;

namespace Application.FeeGenerationRuns
{
    public class FeeGenerationRunService : IFeeGenerationRunService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFeeInvoiceService _feeInvoiceService;

        public FeeGenerationRunService(IUnitOfWork unitOfWork, IFeeInvoiceService feeInvoiceService)
        {
            _unitOfWork = unitOfWork;
            _feeInvoiceService = feeInvoiceService;
        }

        public async Task<CommonResponse<PaginatedResponse<FeeGenerationRunDto>>> GetFeeGenerationRunsAsync(GetFeeGenerationRunsQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new FeeGenerationRunFilter
            {
                AcademicYearId = query.AcademicYearId,
                BillingYear = query.BillingYear,
                BillingMonth = query.BillingMonth
            };

            var pagedRuns = await _unitOfWork.FeeGenerationRuns.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var runDtos = new List<FeeGenerationRunDto>();
            foreach (var run in pagedRuns.Items)
            {
                var periodInvoices = await _unitOfWork.FeeInvoices.GetByPeriodWithDetailsAsync(run.AcademicYearId, run.BillingYear, run.BillingMonth, cancellationToken);
                var runDto = FeeGenerationRunMapper.ToDto(run, periodInvoices);
                runDtos.Add(runDto);
            }

            var paginatedResponse = new PaginatedResponse<FeeGenerationRunDto>
            {
                Items = runDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedRuns.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<FeeGenerationRunDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<FeeGenerationRunDetailDto>> GetFeeGenerationRunByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.FeeGenerationRuns.GetByIdWithYearAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<FeeGenerationRunDetailDto>.Fail(ResponseCodes.NotFound, "Fee generation run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var periodInvoices = await _unitOfWork.FeeInvoices.GetByPeriodWithDetailsAsync(run.AcademicYearId, run.BillingYear, run.BillingMonth, cancellationToken);
            var detailDto = FeeGenerationRunMapper.ToDetailDto(run, periodInvoices);

            var successResponse = CommonResponse<FeeGenerationRunDetailDto>.Success(detailDto);
            return successResponse;
        }

        public async Task<CommonResponse<FeeGenerationClassGroupDto>> GetFeeGenerationRunClassDetailAsync(Guid id, Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.FeeGenerationRuns.GetByIdWithYearAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<FeeGenerationClassGroupDto>.Fail(ResponseCodes.NotFound, "Fee generation run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null || academicClass.AcademicYearId != run.AcademicYearId)
            {
                var notFoundResponse = CommonResponse<FeeGenerationClassGroupDto>.Fail(ResponseCodes.NotFound, "Academic class with id '" + academicClassId + "' was not found in this run's academic year.");
                return notFoundResponse;
            }

            var classInvoices = await _unitOfWork.FeeInvoices.GetByPeriodForClassWithDetailsAsync(run.AcademicYearId, run.BillingYear, run.BillingMonth, academicClassId, cancellationToken);
            var classDto = FeeGenerationRunMapper.ToClassGroupDto(academicClassId, academicClass.GradeCode, classInvoices);

            var successResponse = CommonResponse<FeeGenerationClassGroupDto>.Success(classDto);
            return successResponse;
        }

        // fee_generation_run_refresh_implementation_plan.md -- a thin wrapper around the existing
        // GenerateAsync(RegenerateDrafts: true), not new invoice-building logic: a bulk/individual
        // FeeAdjustment created after a period's invoices already exist sits Pending and inert
        // until generation re-runs for that exact period. This is the discoverable entry point
        // for that re-run, reachable from the run detail page instead of the separate Generate
        // Invoices dialog. Only Draft invoices are ever touched -- GenerateAsync already reports
        // (never touches) anything past Draft.
        public async Task<CommonResponse<FeeGenerationResultDto>> RefreshRunAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.FeeGenerationRuns.GetByIdWithYearAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.NotFound, "Fee generation run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var command = new GenerateFeeInvoicesCommand
            {
                AcademicYearId = run.AcademicYearId,
                BillingYear = run.BillingYear,
                BillingMonth = run.BillingMonth,
                RegenerateDrafts = true
            };

            var generateResponse = await _feeInvoiceService.GenerateAsync(command, cancellationToken);
            return generateResponse;
        }

        public async Task<CommonResponse<FeeGenerationResultDto>> RefreshRunClassAsync(Guid id, Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.FeeGenerationRuns.GetByIdWithYearAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.NotFound, "Fee generation run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null || academicClass.AcademicYearId != run.AcademicYearId)
            {
                var notFoundResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.NotFound, "Academic class with id '" + academicClassId + "' was not found in this run's academic year.");
                return notFoundResponse;
            }

            var command = new GenerateFeeInvoicesCommand
            {
                AcademicYearId = run.AcademicYearId,
                BillingYear = run.BillingYear,
                BillingMonth = run.BillingMonth,
                AcademicClassId = academicClassId,
                RegenerateDrafts = true
            };

            var generateResponse = await _feeInvoiceService.GenerateAsync(command, cancellationToken);
            return generateResponse;
        }
    }
}
