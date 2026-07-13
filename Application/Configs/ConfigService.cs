using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Configs.Commands;
using Application.Configs.Dtos;
using Application.Configs.Queries;
using Application.Configs.Validators;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.Configs
{
    public class ConfigService : IConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateConfigTypeCommandValidator _createConfigTypeCommandValidator;
        private readonly CreateConfigCommandValidator _createConfigCommandValidator;
        private readonly UpdateConfigCommandValidator _updateConfigCommandValidator;
        private readonly UpdateConfigTypeCommandValidator _updateConfigTypeCommandValidator;

        public ConfigService(
            IUnitOfWork unitOfWork,
            CreateConfigTypeCommandValidator createConfigTypeCommandValidator,
            CreateConfigCommandValidator createConfigCommandValidator,
            UpdateConfigCommandValidator updateConfigCommandValidator,
            UpdateConfigTypeCommandValidator updateConfigTypeCommandValidator)
        {
            _unitOfWork = unitOfWork;
            _createConfigTypeCommandValidator = createConfigTypeCommandValidator;
            _createConfigCommandValidator = createConfigCommandValidator;
            _updateConfigCommandValidator = updateConfigCommandValidator;
            _updateConfigTypeCommandValidator = updateConfigTypeCommandValidator;
        }

        public async Task<CommonResponse<ConfigTypeDto>> CreateConfigTypeAsync(CreateConfigTypeCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createConfigTypeCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ConfigTypeDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var typeCodeAlreadyExists = await _unitOfWork.ConfigTypes.TypeCodeExistsAsync(command.TypeCode, cancellationToken);
            if (typeCodeAlreadyExists)
            {
                var conflictMessage = "Config type with type code '" + command.TypeCode + "' already exists.";
                var conflictResponse = CommonResponse<ConfigTypeDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            var configType = new ConfigType
            {
                Id = Guid.NewGuid(),
                TypeCode = command.TypeCode,
                Name = command.Name,
                Description = command.Description
            };

            await _unitOfWork.ConfigTypes.AddAsync(configType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var configTypeDto = ConfigMapper.ToDto(configType);
            var successResponse = CommonResponse<ConfigTypeDto>.Success(configTypeDto, "Config type created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<ConfigTypeDto>>> GetConfigTypesAsync(GetConfigTypesQuery query, CancellationToken cancellationToken = default)
        {
            var pagedConfigTypes = await _unitOfWork.ConfigTypes.GetPagedAsync(query.Page, query.PageSize, cancellationToken);

            var configTypeDtos = new List<ConfigTypeDto>();
            foreach (var configType in pagedConfigTypes.Items)
            {
                var configTypeDto = ConfigMapper.ToDto(configType);
                configTypeDtos.Add(configTypeDto);
            }

            var paginatedResponse = new PaginatedResponse<ConfigTypeDto>
            {
                Items = configTypeDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedConfigTypes.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<ConfigTypeDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<ConfigTypeDto>> GetConfigTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var configType = await _unitOfWork.ConfigTypes.GetByIdAsync(id, cancellationToken);
            if (configType == null)
            {
                var notFoundMessage = "Config type with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<ConfigTypeDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var configTypeDto = ConfigMapper.ToDto(configType);
            var successResponse = CommonResponse<ConfigTypeDto>.Success(configTypeDto);
            return successResponse;
        }

        public async Task<CommonResponse<ConfigTypeDto>> UpdateConfigTypeAsync(Guid id, UpdateConfigTypeCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateConfigTypeCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ConfigTypeDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var configType = await _unitOfWork.ConfigTypes.GetByIdAsync(id, cancellationToken);
            if (configType == null)
            {
                var notFoundMessage = "Config type with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<ConfigTypeDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            configType.Name = command.Name;
            configType.Description = command.Description;

            _unitOfWork.ConfigTypes.Update(configType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var configTypeDto = ConfigMapper.ToDto(configType);
            var successResponse = CommonResponse<ConfigTypeDto>.Success(configTypeDto, "Config type updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteConfigTypeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var configType = await _unitOfWork.ConfigTypes.GetByIdAsync(id, cancellationToken);
            if (configType == null)
            {
                var notFoundMessage = "Config type with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            // The Config -> ConfigType FK is Restrict, so deleting a type that still has options
            // would fail in the database anyway -- surface it as a conflict instead of a 500.
            var hasConfigs = await _unitOfWork.Configs.AnyByTypeCodeAsync(configType.TypeCode, cancellationToken);
            if (hasConfigs)
            {
                var conflictMessage = "Config type with type code '" + configType.TypeCode + "' still has configs. Delete its configs first.";
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            _unitOfWork.ConfigTypes.Remove(configType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Config type deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ConfigDto>> CreateConfigAsync(CreateConfigCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createConfigCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ConfigDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var configTypeExists = await _unitOfWork.ConfigTypes.TypeCodeExistsAsync(command.TypeCode, cancellationToken);
            if (!configTypeExists)
            {
                var notFoundMessage = "Config type with type code '" + command.TypeCode + "' was not found.";
                var notFoundResponse = CommonResponse<ConfigDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var codeAlreadyExists = await _unitOfWork.Configs.CodeExistsAsync(command.TypeCode, command.Code, cancellationToken);
            if (codeAlreadyExists)
            {
                var conflictMessage = "Config code '" + command.Code + "' already exists for type code '" + command.TypeCode + "'.";
                var conflictResponse = CommonResponse<ConfigDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            var config = new Config
            {
                Id = Guid.NewGuid(),
                TypeCode = command.TypeCode,
                Code = command.Code,
                Label = command.Label,
                Order = command.Order,
                AdditionalValue1 = command.AdditionalValue1,
                AdditionalValue2 = command.AdditionalValue2,
                AdditionalValue3 = command.AdditionalValue3
            };

            await _unitOfWork.Configs.AddAsync(config, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var configDto = ConfigMapper.ToDto(config);
            var successResponse = CommonResponse<ConfigDto>.Success(configDto, "Config created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ConfigDto>> GetConfigByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var config = await _unitOfWork.Configs.GetByIdAsync(id, cancellationToken);
            if (config == null)
            {
                var notFoundMessage = "Config with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<ConfigDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var configDto = ConfigMapper.ToDto(config);
            var successResponse = CommonResponse<ConfigDto>.Success(configDto);
            return successResponse;
        }

        public async Task<CommonResponse<ConfigDto>> UpdateConfigAsync(Guid id, UpdateConfigCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateConfigCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ConfigDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var config = await _unitOfWork.Configs.GetByIdAsync(id, cancellationToken);
            if (config == null)
            {
                var notFoundMessage = "Config with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<ConfigDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var codeIsChanging = !string.Equals(config.Code, command.Code, StringComparison.Ordinal);
            if (codeIsChanging)
            {
                var codeAlreadyExists = await _unitOfWork.Configs.CodeExistsAsync(config.TypeCode, command.Code, cancellationToken);
                if (codeAlreadyExists)
                {
                    var conflictMessage = "Config code '" + command.Code + "' already exists for type code '" + config.TypeCode + "'.";
                    var conflictResponse = CommonResponse<ConfigDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                    return conflictResponse;
                }
            }

            config.Code = command.Code;
            config.Label = command.Label;
            config.Order = command.Order;
            config.AdditionalValue1 = command.AdditionalValue1;
            config.AdditionalValue2 = command.AdditionalValue2;
            config.AdditionalValue3 = command.AdditionalValue3;

            _unitOfWork.Configs.Update(config);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var configDto = ConfigMapper.ToDto(config);
            var successResponse = CommonResponse<ConfigDto>.Success(configDto, "Config updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteConfigAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var config = await _unitOfWork.Configs.GetByIdAsync(id, cancellationToken);
            if (config == null)
            {
                var notFoundMessage = "Config with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            _unitOfWork.Configs.Remove(config);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Config deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<DropdownItemDto>>> GetConfigsByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default)
        {
            var configs = await _unitOfWork.Configs.GetByTypeCodeAsync(typeCode, cancellationToken);

            var dropdownItemDtos = new List<DropdownItemDto>();
            foreach (var config in configs)
            {
                var dropdownItemDto = ConfigMapper.ToDropdownItemDto(config);
                dropdownItemDtos.Add(dropdownItemDto);
            }

            var successResponse = CommonResponse<List<DropdownItemDto>>.Success(dropdownItemDtos);
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
