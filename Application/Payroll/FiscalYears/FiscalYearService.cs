using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Payroll.FiscalYears.Commands;
using Application.Payroll.FiscalYears.Dtos;
using Application.Payroll.FiscalYears.Queries;
using Application.Payroll.FiscalYears.Validators;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Payroll.FiscalYears
{
    public class FiscalYearService : IFiscalYearService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateFiscalYearCommandValidator _createValidator;
        private readonly UpdateFiscalYearCommandValidator _updateValidator;
        private readonly CreateTaxSlabCommandValidator _createTaxSlabValidator;
        private readonly UpdateTaxSlabCommandValidator _updateTaxSlabValidator;

        public FiscalYearService(
            IUnitOfWork unitOfWork,
            CreateFiscalYearCommandValidator createValidator,
            UpdateFiscalYearCommandValidator updateValidator,
            CreateTaxSlabCommandValidator createTaxSlabValidator,
            UpdateTaxSlabCommandValidator updateTaxSlabValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _createTaxSlabValidator = createTaxSlabValidator;
            _updateTaxSlabValidator = updateTaxSlabValidator;
        }

        public async Task<CommonResponse<FiscalYearDto>> CreateFiscalYearAsync(CreateFiscalYearCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FiscalYearDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var trimmedCode = command.Code.Trim();
            var codeAlreadyExists = await _unitOfWork.FiscalYears.CodeExistsAsync(trimmedCode, cancellationToken);
            if (codeAlreadyExists)
            {
                var conflictResponse = CommonResponse<FiscalYearDto>.Fail(ResponseCodes.Conflict, "Fiscal year code '" + trimmedCode + "' is already in use (possibly by a soft-deleted year).");
                return conflictResponse;
            }

            if (command.IsCurrent)
            {
                await UnsetCurrentYearsAsync(cancellationToken);
            }

            var fiscalYear = new FiscalYear
            {
                Code = trimmedCode,
                Name = command.Name.Trim(),
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                IsCurrent = command.IsCurrent,
                Status = RecordStatus.Active,
                RetirementExemptionCapAmount = command.RetirementExemptionCapAmount
            };

            await _unitOfWork.FiscalYears.AddAsync(fiscalYear, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var fiscalYearDto = FiscalYearMapper.ToDto(fiscalYear);
            var successResponse = CommonResponse<FiscalYearDto>.Success(fiscalYearDto, "Fiscal year created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FiscalYearDto>> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
            if (fiscalYear == null)
            {
                var notFoundResponse = CommonResponse<FiscalYearDto>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var fiscalYearDto = FiscalYearMapper.ToDto(fiscalYear);
            var successResponse = CommonResponse<FiscalYearDto>.Success(fiscalYearDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<FiscalYearDto>>> GetFiscalYearsAsync(GetFiscalYearsQuery query, CancellationToken cancellationToken = default)
        {
            var pagedYears = await _unitOfWork.FiscalYears.GetPagedOrderedAsync(query.Page, query.PageSize, cancellationToken);

            var fiscalYearDtos = new List<FiscalYearDto>();
            foreach (var fiscalYear in pagedYears.Items)
            {
                var fiscalYearDto = FiscalYearMapper.ToDto(fiscalYear);
                fiscalYearDtos.Add(fiscalYearDto);
            }

            var paginatedResponse = new PaginatedResponse<FiscalYearDto>
            {
                Items = fiscalYearDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedYears.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<FiscalYearDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<FiscalYearDto>> UpdateFiscalYearAsync(Guid id, UpdateFiscalYearCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FiscalYearDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
            if (fiscalYear == null)
            {
                var notFoundResponse = CommonResponse<FiscalYearDto>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var becomingCurrent = command.IsCurrent && !fiscalYear.IsCurrent;
            if (becomingCurrent)
            {
                await UnsetCurrentYearsAsync(cancellationToken);
            }

            fiscalYear.Name = command.Name.Trim();
            fiscalYear.StartDate = command.StartDate;
            fiscalYear.EndDate = command.EndDate;
            fiscalYear.IsCurrent = command.IsCurrent;
            fiscalYear.Status = command.Status;
            fiscalYear.RetirementExemptionCapAmount = command.RetirementExemptionCapAmount;

            _unitOfWork.FiscalYears.Update(fiscalYear);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var fiscalYearDto = FiscalYearMapper.ToDto(fiscalYear);
            var successResponse = CommonResponse<FiscalYearDto>.Success(fiscalYearDto, "Fiscal year updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteFiscalYearAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
            if (fiscalYear == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var hasTaxSlabs = await _unitOfWork.FiscalYears.HasTaxSlabsAsync(id, cancellationToken);
            if (hasTaxSlabs)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This fiscal year still has tax slabs. Remove them first.");
                return conflictResponse;
            }

            _unitOfWork.FiscalYears.Remove(fiscalYear);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Fiscal year deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<TaxSlabDto>> AddTaxSlabAsync(Guid fiscalYearId, CreateTaxSlabCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createTaxSlabValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TaxSlabDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(fiscalYearId, cancellationToken);
            if (fiscalYear == null)
            {
                var notFoundResponse = CommonResponse<TaxSlabDto>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + fiscalYearId + "' was not found.");
                return notFoundResponse;
            }

            var taxSlab = new TaxSlab
            {
                FiscalYearId = fiscalYearId,
                AssessmentType = command.AssessmentType,
                MinAmount = command.MinAmount,
                MaxAmount = command.MaxAmount,
                TaxRate = command.TaxRate,
                SlabOrder = command.SlabOrder,
                FiscalYear = fiscalYear
            };

            await _unitOfWork.FiscalYears.AddTaxSlabAsync(taxSlab, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var taxSlabDto = FiscalYearMapper.ToTaxSlabDto(taxSlab);
            var successResponse = CommonResponse<TaxSlabDto>.Success(taxSlabDto, "Tax slab added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<TaxSlabDto>> UpdateTaxSlabAsync(Guid fiscalYearId, Guid taxSlabId, UpdateTaxSlabCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateTaxSlabValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TaxSlabDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var taxSlab = await _unitOfWork.FiscalYears.GetTaxSlabByIdAsync(taxSlabId, cancellationToken);
            if (taxSlab == null || taxSlab.FiscalYearId != fiscalYearId)
            {
                var notFoundResponse = CommonResponse<TaxSlabDto>.Fail(ResponseCodes.NotFound, "Tax slab was not found on this fiscal year.");
                return notFoundResponse;
            }

            taxSlab.MinAmount = command.MinAmount;
            taxSlab.MaxAmount = command.MaxAmount;
            taxSlab.TaxRate = command.TaxRate;
            taxSlab.SlabOrder = command.SlabOrder;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var taxSlabDto = FiscalYearMapper.ToTaxSlabDto(taxSlab);
            var successResponse = CommonResponse<TaxSlabDto>.Success(taxSlabDto, "Tax slab updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveTaxSlabAsync(Guid fiscalYearId, Guid taxSlabId, CancellationToken cancellationToken = default)
        {
            var taxSlab = await _unitOfWork.FiscalYears.GetTaxSlabByIdAsync(taxSlabId, cancellationToken);
            if (taxSlab == null || taxSlab.FiscalYearId != fiscalYearId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Tax slab was not found on this fiscal year.");
                return notFoundResponse;
            }

            _unitOfWork.FiscalYears.RemoveTaxSlab(taxSlab);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Tax slab removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<TaxSlabDto>>> GetTaxSlabsAsync(Guid fiscalYearId, CancellationToken cancellationToken = default)
        {
            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(fiscalYearId, cancellationToken);
            if (fiscalYear == null)
            {
                var notFoundResponse = CommonResponse<List<TaxSlabDto>>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + fiscalYearId + "' was not found.");
                return notFoundResponse;
            }

            var taxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYearId, null, cancellationToken);

            var taxSlabDtos = new List<TaxSlabDto>();
            foreach (var taxSlab in taxSlabs)
            {
                var taxSlabDto = FiscalYearMapper.ToTaxSlabDto(taxSlab);
                taxSlabDtos.Add(taxSlabDto);
            }

            var successResponse = CommonResponse<List<TaxSlabDto>>.Success(taxSlabDtos);
            return successResponse;
        }

        private async Task UnsetCurrentYearsAsync(CancellationToken cancellationToken)
        {
            var currentYears = await _unitOfWork.FiscalYears.GetCurrentYearsAsync(cancellationToken);
            foreach (var currentYear in currentYears)
            {
                currentYear.IsCurrent = false;
                _unitOfWork.FiscalYears.Update(currentYear);
            }
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
