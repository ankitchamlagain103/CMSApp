using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Guardians.Commands;
using Application.Guardians.Dtos;
using Application.Guardians.Validators;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.Guardians
{
    public class GuardianService : IGuardianService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateGuardianCommandValidator _createValidator;
        private readonly UpdateGuardianCommandValidator _updateValidator;

        public GuardianService(
            IUnitOfWork unitOfWork,
            CreateGuardianCommandValidator createValidator,
            UpdateGuardianCommandValidator updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<CommonResponse<GuardianDto>> CreateGuardianAsync(CreateGuardianCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<GuardianDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var guardian = new Guardian
            {
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                Email = command.Email?.Trim(),
                Phone = command.Phone?.Trim(),
                Occupation = command.Occupation?.Trim(),
                Address = command.Address?.Trim()
            };

            await _unitOfWork.Guardians.AddAsync(guardian, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var guardianDto = GuardianMapper.ToDto(guardian);
            var successResponse = CommonResponse<GuardianDto>.Success(guardianDto, "Guardian created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<GuardianDto>> GetGuardianByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var guardian = await _unitOfWork.Guardians.GetByIdAsync(id, cancellationToken);
            if (guardian == null)
            {
                var notFoundResponse = CommonResponse<GuardianDto>.Fail(ResponseCodes.NotFound, "Guardian with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var guardianDto = GuardianMapper.ToDto(guardian);
            var successResponse = CommonResponse<GuardianDto>.Success(guardianDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<GuardianDto>>> GetGuardiansAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedGuardians = await _unitOfWork.Guardians.GetPagedOrderedAsync(page, pageSize, cancellationToken);

            var guardianDtos = new List<GuardianDto>();
            foreach (var guardian in pagedGuardians.Items)
            {
                var guardianDto = GuardianMapper.ToDto(guardian);
                guardianDtos.Add(guardianDto);
            }

            var paginatedResponse = new PaginatedResponse<GuardianDto>
            {
                Items = guardianDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedGuardians.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<GuardianDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<GuardianDto>> UpdateGuardianAsync(Guid id, UpdateGuardianCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<GuardianDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var guardian = await _unitOfWork.Guardians.GetByIdAsync(id, cancellationToken);
            if (guardian == null)
            {
                var notFoundResponse = CommonResponse<GuardianDto>.Fail(ResponseCodes.NotFound, "Guardian with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            guardian.FirstName = command.FirstName.Trim();
            guardian.LastName = command.LastName.Trim();
            guardian.Email = command.Email?.Trim();
            guardian.Phone = command.Phone?.Trim();
            guardian.Occupation = command.Occupation?.Trim();
            guardian.Address = command.Address?.Trim();

            _unitOfWork.Guardians.Update(guardian);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var guardianDto = GuardianMapper.ToDto(guardian);
            var successResponse = CommonResponse<GuardianDto>.Success(guardianDto, "Guardian updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteGuardianAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var guardian = await _unitOfWork.Guardians.GetByIdAsync(id, cancellationToken);
            if (guardian == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Guardian with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var hasStudentLinks = await _unitOfWork.Guardians.HasStudentLinksAsync(id, cancellationToken);
            if (hasStudentLinks)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This guardian is still linked to students. Unlink them first.");
                return conflictResponse;
            }

            _unitOfWork.Guardians.Remove(guardian);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Guardian deleted successfully.");
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
