using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Fees.Commands;
using Application.Fees.Dtos;
using Application.Fees.Queries;
using Application.Fees.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Fees
{
    public class FeeStructureService : IFeeStructureService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateFeeStructureCommandValidator _createValidator;
        private readonly UpdateFeeStructureCommandValidator _updateValidator;
        private readonly FeeStructureItemInputValidator _itemInputValidator;
        private readonly UpdateFeeStructureItemCommandValidator _updateItemValidator;

        public FeeStructureService(
            IUnitOfWork unitOfWork,
            CreateFeeStructureCommandValidator createValidator,
            UpdateFeeStructureCommandValidator updateValidator,
            FeeStructureItemInputValidator itemInputValidator,
            UpdateFeeStructureItemCommandValidator updateItemValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _itemInputValidator = itemInputValidator;
            _updateItemValidator = updateItemValidator;
        }

        public async Task<CommonResponse<FeeStructureDto>> CreateFeeStructureAsync(CreateFeeStructureCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(command.AcademicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.NotFound, "Class with id '" + command.AcademicClassId + "' was not found.");
                return notFoundResponse;
            }

            var alreadyExists = await _unitOfWork.FeeStructures.ExistsForAcademicClassAsync(command.AcademicClassId, cancellationToken);
            if (alreadyExists)
            {
                var conflictResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.Conflict, "A fee structure for this class already exists (possibly soft-deleted). Update it or add items to it instead.");
                return conflictResponse;
            }

            var seenCategoryCodes = new List<string>();
            foreach (var itemInput in command.Items)
            {
                var itemCategoryCode = itemInput.FeeCategoryCode.Trim();

                if (seenCategoryCodes.Contains(itemCategoryCode))
                {
                    var duplicateResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.ValidationError, "FeeCategoryCode '" + itemCategoryCode + "' was submitted more than once.");
                    return duplicateResponse;
                }

                seenCategoryCodes.Add(itemCategoryCode);

                var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, itemCategoryCode, cancellationToken);
                if (!categoryExists)
                {
                    var invalidCategoryResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.ValidationError, "FeeCategoryCode '" + itemCategoryCode + "' is not a known fee category option.");
                    return invalidCategoryResponse;
                }
            }

            var feeStructure = new FeeStructure
            {
                AcademicClassId = command.AcademicClassId,
                Status = RecordStatus.Active,
                AcademicClass = academicClass
            };

            foreach (var itemInput in command.Items)
            {
                feeStructure.Items.Add(new FeeStructureItem
                {
                    FeeCategoryCode = itemInput.FeeCategoryCode.Trim(),
                    Amount = itemInput.Amount,
                    FrequencyType = itemInput.FrequencyType,
                    InstallmentCount = itemInput.InstallmentCount,
                    IsOptional = itemInput.IsOptional,
                    IsRefundable = itemInput.IsRefundable,
                    FeeStructure = feeStructure
                });
            }

            await _unitOfWork.FeeStructures.AddAsync(feeStructure, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var categoryLabels = await LoadFeeCategoryLabelMapAsync(cancellationToken);
            var feeStructureDto = FeeStructureMapper.ToDto(feeStructure, categoryLabels);
            var successResponse = CommonResponse<FeeStructureDto>.Success(feeStructureDto, "Fee structure created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeStructureDto>> GetFeeStructureByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var feeStructure = await _unitOfWork.FeeStructures.GetByIdWithItemsAsync(id, cancellationToken);
            if (feeStructure == null)
            {
                var notFoundResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.NotFound, "Fee structure with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var categoryLabels = await LoadFeeCategoryLabelMapAsync(cancellationToken);
            var feeStructureDto = FeeStructureMapper.ToDto(feeStructure, categoryLabels);
            var successResponse = CommonResponse<FeeStructureDto>.Success(feeStructureDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<FeeStructureDto>>> GetFeeStructuresAsync(GetFeeStructuresQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new FeeStructureFilter
            {
                AcademicYearId = query.AcademicYearId,
                AcademicClassId = query.AcademicClassId,
                Status = query.Status
            };

            var pagedFeeStructures = await _unitOfWork.FeeStructures.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);
            var categoryLabels = await LoadFeeCategoryLabelMapAsync(cancellationToken);

            var feeStructureDtos = new List<FeeStructureDto>();
            foreach (var feeStructure in pagedFeeStructures.Items)
            {
                var feeStructureDto = FeeStructureMapper.ToDto(feeStructure, categoryLabels);
                feeStructureDtos.Add(feeStructureDto);
            }

            var paginatedResponse = new PaginatedResponse<FeeStructureDto>
            {
                Items = feeStructureDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedFeeStructures.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<FeeStructureDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<FeeStructureDto>> UpdateFeeStructureAsync(Guid id, UpdateFeeStructureCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var feeStructure = await _unitOfWork.FeeStructures.GetByIdWithItemsAsync(id, cancellationToken);
            if (feeStructure == null)
            {
                var notFoundResponse = CommonResponse<FeeStructureDto>.Fail(ResponseCodes.NotFound, "Fee structure with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            feeStructure.Status = command.Status;

            _unitOfWork.FeeStructures.Update(feeStructure);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var categoryLabels = await LoadFeeCategoryLabelMapAsync(cancellationToken);
            var feeStructureDto = FeeStructureMapper.ToDto(feeStructure, categoryLabels);
            var successResponse = CommonResponse<FeeStructureDto>.Success(feeStructureDto, "Fee structure updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteFeeStructureAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var feeStructure = await _unitOfWork.FeeStructures.GetByIdWithItemsAsync(id, cancellationToken);
            if (feeStructure == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Fee structure with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            foreach (var item in feeStructure.Items)
            {
                var referenced = await _unitOfWork.Enrollments.FeeSelectionExistsForItemAsync(item.Id, cancellationToken);
                if (referenced)
                {
                    var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This fee structure still has items that enrollments have opted into. Remove those selections first.");
                    return conflictResponse;
                }
            }

            _unitOfWork.FeeStructures.Remove(feeStructure);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Fee structure deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeStructureItemDto>> AddItemAsync(Guid feeStructureId, FeeStructureItemInput command, CancellationToken cancellationToken = default)
        {
            var validationResult = _itemInputValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeStructureItemDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var feeStructure = await _unitOfWork.FeeStructures.GetByIdWithItemsAsync(feeStructureId, cancellationToken);
            if (feeStructure == null)
            {
                var notFoundResponse = CommonResponse<FeeStructureItemDto>.Fail(ResponseCodes.NotFound, "Fee structure with id '" + feeStructureId + "' was not found.");
                return notFoundResponse;
            }

            var categoryCode = command.FeeCategoryCode.Trim();

            var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, categoryCode, cancellationToken);
            if (!categoryExists)
            {
                var invalidCategoryResponse = CommonResponse<FeeStructureItemDto>.Fail(ResponseCodes.ValidationError, "FeeCategoryCode '" + categoryCode + "' is not a known fee category option.");
                return invalidCategoryResponse;
            }

            foreach (var existingItem in feeStructure.Items)
            {
                if (existingItem.FeeCategoryCode == categoryCode)
                {
                    var conflictResponse = CommonResponse<FeeStructureItemDto>.Fail(ResponseCodes.Conflict, "This fee structure already charges category '" + categoryCode + "'.");
                    return conflictResponse;
                }
            }

            var item = new FeeStructureItem
            {
                FeeStructureId = feeStructureId,
                FeeCategoryCode = categoryCode,
                Amount = command.Amount,
                FrequencyType = command.FrequencyType,
                InstallmentCount = command.InstallmentCount,
                IsOptional = command.IsOptional,
                IsRefundable = command.IsRefundable,
                FeeStructure = feeStructure
            };

            await _unitOfWork.FeeStructures.AddItemAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var categoryLabels = await LoadFeeCategoryLabelMapAsync(cancellationToken);
            var itemDto = FeeStructureMapper.ToItemDto(item, categoryLabels);
            var successResponse = CommonResponse<FeeStructureItemDto>.Success(itemDto, "Fee structure item added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeStructureItemDto>> UpdateItemAsync(Guid feeStructureId, Guid itemId, UpdateFeeStructureItemCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateItemValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeStructureItemDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var item = await _unitOfWork.FeeStructures.GetItemByIdAsync(itemId, cancellationToken);
            if (item == null || item.FeeStructureId != feeStructureId)
            {
                var notFoundResponse = CommonResponse<FeeStructureItemDto>.Fail(ResponseCodes.NotFound, "Fee structure item was not found on this fee structure.");
                return notFoundResponse;
            }

            item.Amount = command.Amount;
            item.FrequencyType = command.FrequencyType;
            item.InstallmentCount = command.InstallmentCount;
            item.IsOptional = command.IsOptional;
            item.IsRefundable = command.IsRefundable;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var categoryLabels = await LoadFeeCategoryLabelMapAsync(cancellationToken);
            var itemDto = FeeStructureMapper.ToItemDto(item, categoryLabels);
            var successResponse = CommonResponse<FeeStructureItemDto>.Success(itemDto, "Fee structure item updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveItemAsync(Guid feeStructureId, Guid itemId, CancellationToken cancellationToken = default)
        {
            var item = await _unitOfWork.FeeStructures.GetItemByIdAsync(itemId, cancellationToken);
            if (item == null || item.FeeStructureId != feeStructureId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Fee structure item was not found on this fee structure.");
                return notFoundResponse;
            }

            var referenced = await _unitOfWork.Enrollments.FeeSelectionExistsForItemAsync(itemId, cancellationToken);
            if (referenced)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This fee item still has enrollments opted into it. Remove those selections first.");
                return conflictResponse;
            }

            _unitOfWork.FeeStructures.RemoveItem(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Fee structure item removed successfully.");
            return successResponse;
        }

        // FeeCategory (1010) code -> Label map so every fee-structure DTO carries the
        // human-readable category label alongside the stored code (2026-07-19).
        private async Task<Dictionary<string, string>> LoadFeeCategoryLabelMapAsync(CancellationToken cancellationToken)
        {
            var options = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.FeeCategory, cancellationToken);
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(options);
            return labelsByCode;
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
