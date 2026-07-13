using Application.AppConfigs.Commands;
using Application.AppConfigs.Dtos;
using Application.AppConfigs.Queries;
using Application.AppConfigs.Validators;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.AppConfigs
{
    public class AppConfigService : IAppConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateAppConfigCommandValidator _createAppConfigCommandValidator;
        private readonly UpdateAppConfigCommandValidator _updateAppConfigCommandValidator;

        public AppConfigService(
            IUnitOfWork unitOfWork,
            CreateAppConfigCommandValidator createAppConfigCommandValidator,
            UpdateAppConfigCommandValidator updateAppConfigCommandValidator)
        {
            _unitOfWork = unitOfWork;
            _createAppConfigCommandValidator = createAppConfigCommandValidator;
            _updateAppConfigCommandValidator = updateAppConfigCommandValidator;
        }

        public async Task<CommonResponse<AppConfigDto>> CreateAppConfigAsync(CreateAppConfigCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createAppConfigCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AppConfigDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var configParamAlreadyExists = await _unitOfWork.AppConfigs.ConfigParamExistsAsync(command.ConfigParam, cancellationToken);
            if (configParamAlreadyExists)
            {
                var conflictMessage = "App config with param '" + command.ConfigParam + "' already exists.";
                var conflictResponse = CommonResponse<AppConfigDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            var appConfig = new AppConfig
            {
                Id = Guid.NewGuid(),
                ConfigParam = command.ConfigParam,
                ConfigValue = command.ConfigValue,
                ConfigGroup = command.ConfigGroup,
                IsEnable = command.IsEnable
            };

            await _unitOfWork.AppConfigs.AddAsync(appConfig, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var appConfigDto = AppConfigMapper.ToDto(appConfig);
            var successResponse = CommonResponse<AppConfigDto>.Success(appConfigDto, "App config created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<AppConfigDto>>> GetAppConfigsAsync(GetAppConfigsQuery query, CancellationToken cancellationToken = default)
        {
            var pagedAppConfigs = await _unitOfWork.AppConfigs.GetPagedAsync(query.Page, query.PageSize, cancellationToken);

            var appConfigDtos = new List<AppConfigDto>();
            foreach (var appConfig in pagedAppConfigs.Items)
            {
                var appConfigDto = AppConfigMapper.ToDto(appConfig);
                appConfigDtos.Add(appConfigDto);
            }

            var paginatedResponse = new PaginatedResponse<AppConfigDto>
            {
                Items = appConfigDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedAppConfigs.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<AppConfigDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<AppConfigDto>> GetAppConfigByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appConfig = await _unitOfWork.AppConfigs.GetByIdAsync(id, cancellationToken);
            if (appConfig == null)
            {
                var notFoundMessage = "App config with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<AppConfigDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var appConfigDto = AppConfigMapper.ToDto(appConfig);
            var successResponse = CommonResponse<AppConfigDto>.Success(appConfigDto);
            return successResponse;
        }

        public async Task<CommonResponse<List<AppConfigDto>>> GetAppConfigsByGroupAsync(string configGroup, CancellationToken cancellationToken = default)
        {
            var appConfigs = await _unitOfWork.AppConfigs.GetByGroupAsync(configGroup, cancellationToken);

            var appConfigDtos = new List<AppConfigDto>();
            foreach (var appConfig in appConfigs)
            {
                var appConfigDto = AppConfigMapper.ToDto(appConfig);
                appConfigDtos.Add(appConfigDto);
            }

            var successResponse = CommonResponse<List<AppConfigDto>>.Success(appConfigDtos);
            return successResponse;
        }

        public async Task<CommonResponse<AppConfigDto>> UpdateAppConfigAsync(Guid id, UpdateAppConfigCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateAppConfigCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AppConfigDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var appConfig = await _unitOfWork.AppConfigs.GetByIdAsync(id, cancellationToken);
            if (appConfig == null)
            {
                var notFoundMessage = "App config with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<AppConfigDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var configParamIsChanging = !string.Equals(appConfig.ConfigParam, command.ConfigParam, StringComparison.Ordinal);
            if (configParamIsChanging)
            {
                var configParamAlreadyExists = await _unitOfWork.AppConfigs.ConfigParamExistsAsync(command.ConfigParam, cancellationToken);
                if (configParamAlreadyExists)
                {
                    var conflictMessage = "App config with param '" + command.ConfigParam + "' already exists.";
                    var conflictResponse = CommonResponse<AppConfigDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                    return conflictResponse;
                }
            }

            appConfig.ConfigParam = command.ConfigParam;
            appConfig.ConfigValue = command.ConfigValue;
            appConfig.ConfigGroup = command.ConfigGroup;
            appConfig.IsEnable = command.IsEnable;

            _unitOfWork.AppConfigs.Update(appConfig);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var appConfigDto = AppConfigMapper.ToDto(appConfig);
            var successResponse = CommonResponse<AppConfigDto>.Success(appConfigDto, "App config updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteAppConfigAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var appConfig = await _unitOfWork.AppConfigs.GetByIdAsync(id, cancellationToken);
            if (appConfig == null)
            {
                var notFoundMessage = "App config with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            _unitOfWork.AppConfigs.Remove(appConfig);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "App config deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<PublicAppConfigDto>>> GetPublicAppConfigsAsync(CancellationToken cancellationToken = default)
        {
            var enabledAppConfigs = await _unitOfWork.AppConfigs.GetEnabledAsync(cancellationToken);

            var publicAppConfigDtos = new List<PublicAppConfigDto>();
            foreach (var appConfig in enabledAppConfigs)
            {
                var publicAppConfigDto = AppConfigMapper.ToPublicDto(appConfig);
                publicAppConfigDtos.Add(publicAppConfigDto);
            }

            var successResponse = CommonResponse<List<PublicAppConfigDto>>.Success(publicAppConfigDtos);
            return successResponse;
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
