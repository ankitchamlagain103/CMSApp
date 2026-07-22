using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.FeeRules.Commands;
using Application.FeeRules.Dtos;
using Application.FeeRules.Queries;
using Application.FeeRules.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.FeeRules
{
    public class FeeRuleService : IFeeRuleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateFeeRuleCommandValidator _createValidator;
        private readonly UpdateFeeRuleCommandValidator _updateValidator;

        public FeeRuleService(
            IUnitOfWork unitOfWork,
            CreateFeeRuleCommandValidator createValidator,
            UpdateFeeRuleCommandValidator updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<CommonResponse<FeeRuleDto>> CreateFeeRuleAsync(CreateFeeRuleCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var code = command.Code.Trim();
            var codeExists = await _unitOfWork.FeeRules.CodeExistsAsync(code, cancellationToken);
            if (codeExists)
            {
                var conflictResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.Conflict, "Fee rule code '" + code + "' is already in use (possibly by a soft-deleted rule).");
                return conflictResponse;
            }

            var scopeErrorMessage = await ValidateScopeAsync(command.AcademicClassId, command.FeeCategoryCode, cancellationToken);
            if (scopeErrorMessage != null)
            {
                var scopeFailureResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.ValidationError, scopeErrorMessage);
                return scopeFailureResponse;
            }

            var rule = new FeeRule
            {
                Code = code,
                Name = command.Name.Trim(),
                RuleType = command.RuleType,
                TriggerStage = ResolveTriggerStage(command.RuleType),
                ValueType = command.ValueType,
                Value = command.Value,
                MinMonthsTogether = command.RuleType == FeeRuleType.AdvanceMonthsDiscount ? command.MinMonthsTogether : null,
                DaysBeforeDueDate = command.RuleType == FeeRuleType.EarlyPaymentDiscount ? command.DaysBeforeDueDate : null,
                AcademicClassId = command.AcademicClassId,
                FeeCategoryCode = string.IsNullOrWhiteSpace(command.FeeCategoryCode) ? null : command.FeeCategoryCode.Trim(),
                EffectiveFrom = DateTimeHelper.AsUtcDate(command.EffectiveFrom),
                EffectiveTo = DateTimeHelper.AsUtcDate(command.EffectiveTo),
                Priority = command.Priority,
                IsCombinable = command.IsCombinable,
                IsActive = command.IsActive
            };

            await _unitOfWork.FeeRules.AddAsync(rule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var ruleDto = FeeRuleMapper.ToDto(rule);
            var successResponse = CommonResponse<FeeRuleDto>.Success(ruleDto, "Fee rule created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeRuleDto>> GetFeeRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var rule = await _unitOfWork.FeeRules.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                var notFoundResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.NotFound, "Fee rule with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var ruleDto = FeeRuleMapper.ToDto(rule);
            var successResponse = CommonResponse<FeeRuleDto>.Success(ruleDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<FeeRuleDto>>> GetFeeRulesAsync(GetFeeRulesQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new FeeRuleFilter
            {
                RuleType = query.RuleType,
                AcademicClassId = query.AcademicClassId,
                IsActive = query.IsActive
            };

            var pagedRules = await _unitOfWork.FeeRules.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var ruleDtos = new List<FeeRuleDto>();
            foreach (var rule in pagedRules.Items)
            {
                var ruleDto = FeeRuleMapper.ToDto(rule);
                ruleDtos.Add(ruleDto);
            }

            var paginatedResponse = new PaginatedResponse<FeeRuleDto>
            {
                Items = ruleDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedRules.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<FeeRuleDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<FeeRuleDto>> UpdateFeeRuleAsync(Guid id, UpdateFeeRuleCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var rule = await _unitOfWork.FeeRules.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                var notFoundResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.NotFound, "Fee rule with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            // RuleType is immutable, so the per-type parameter requirement is re-checked here
            // against the existing type.
            if (rule.RuleType == FeeRuleType.AdvanceMonthsDiscount && (!command.MinMonthsTogether.HasValue || command.MinMonthsTogether.Value < 2))
            {
                var missingMonthsResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.ValidationError, "MinMonthsTogether (at least 2) is required for an advance-months discount rule.");
                return missingMonthsResponse;
            }

            if (rule.RuleType == FeeRuleType.EarlyPaymentDiscount && (!command.DaysBeforeDueDate.HasValue || command.DaysBeforeDueDate.Value < 0))
            {
                var missingDaysResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.ValidationError, "DaysBeforeDueDate (0 or more) is required for an early-payment discount rule.");
                return missingDaysResponse;
            }

            var scopeErrorMessage = await ValidateScopeAsync(command.AcademicClassId, command.FeeCategoryCode, cancellationToken);
            if (scopeErrorMessage != null)
            {
                var scopeFailureResponse = CommonResponse<FeeRuleDto>.Fail(ResponseCodes.ValidationError, scopeErrorMessage);
                return scopeFailureResponse;
            }

            rule.Name = command.Name.Trim();
            rule.ValueType = command.ValueType;
            rule.Value = command.Value;
            rule.MinMonthsTogether = rule.RuleType == FeeRuleType.AdvanceMonthsDiscount ? command.MinMonthsTogether : null;
            rule.DaysBeforeDueDate = rule.RuleType == FeeRuleType.EarlyPaymentDiscount ? command.DaysBeforeDueDate : null;
            rule.AcademicClassId = command.AcademicClassId;
            rule.FeeCategoryCode = string.IsNullOrWhiteSpace(command.FeeCategoryCode) ? null : command.FeeCategoryCode.Trim();
            rule.EffectiveFrom = DateTimeHelper.AsUtcDate(command.EffectiveFrom);
            rule.EffectiveTo = DateTimeHelper.AsUtcDate(command.EffectiveTo);
            rule.Priority = command.Priority;
            rule.IsCombinable = command.IsCombinable;
            rule.IsActive = command.IsActive;

            _unitOfWork.FeeRules.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var ruleDto = FeeRuleMapper.ToDto(rule);
            var successResponse = CommonResponse<FeeRuleDto>.Success(ruleDto, "Fee rule updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteFeeRuleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var rule = await _unitOfWork.FeeRules.GetByIdAsync(id, cancellationToken);
            if (rule == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Fee rule with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            // Soft delete; historical RuleDiscount invoice lines keep their FeeRuleId lineage
            // (plain scalar, no FK), so no child guard is needed.
            _unitOfWork.FeeRules.Remove(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Fee rule deleted successfully.");
            return successResponse;
        }

        private async Task<string> ValidateScopeAsync(Guid? academicClassId, string feeCategoryCode, CancellationToken cancellationToken)
        {
            if (academicClassId.HasValue)
            {
                var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId.Value, cancellationToken);
                if (academicClass == null)
                {
                    return "Class with id '" + academicClassId.Value + "' was not found.";
                }
            }

            if (!string.IsNullOrWhiteSpace(feeCategoryCode))
            {
                var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, feeCategoryCode.Trim(), cancellationToken);
                if (!categoryExists)
                {
                    return "FeeCategoryCode '" + feeCategoryCode.Trim() + "' is not a known fee category option.";
                }
            }

            return null;
        }

        private static FeeRuleTrigger ResolveTriggerStage(FeeRuleType ruleType)
        {
            // Both shipped rule types depend on how a payment settles invoices; future
            // generation-time rules add their mapping here.
            return FeeRuleTrigger.OnPayment;
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
