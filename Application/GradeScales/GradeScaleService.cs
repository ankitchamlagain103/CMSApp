using Application.Common.Interfaces;
using Application.Common.Models;
using Application.GradeScales.Commands;
using Application.GradeScales.Dtos;
using Application.GradeScales.Validators;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.GradeScales
{
    public class GradeScaleService : IGradeScaleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateGradeScaleCommandValidator _createValidator;
        private readonly UpdateGradeScaleCommandValidator _updateValidator;

        public GradeScaleService(
            IUnitOfWork unitOfWork,
            CreateGradeScaleCommandValidator createValidator,
            UpdateGradeScaleCommandValidator updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<CommonResponse<GradeScaleDto>> CreateGradeScaleAsync(CreateGradeScaleCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<GradeScaleDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var grade = command.Grade.Trim();
            var gradeExists = await _unitOfWork.GradeScales.GradeExistsAsync(grade, null, cancellationToken);
            if (gradeExists)
            {
                var conflictResponse = CommonResponse<GradeScaleDto>.Fail(ResponseCodes.Conflict, "Grade '" + grade + "' already exists (possibly soft-deleted).");
                return conflictResponse;
            }

            var gradeScale = new GradeScale
            {
                Grade = grade,
                MinPercent = command.MinPercent,
                MaxPercent = command.MaxPercent,
                GradePoint = command.GradePoint,
                Remarks = command.Remarks
            };

            await _unitOfWork.GradeScales.AddAsync(gradeScale, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var gradeScaleDto = GradeScaleMapper.ToDto(gradeScale);
            var successResponse = CommonResponse<GradeScaleDto>.Success(gradeScaleDto, "Grade scale created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<GradeScaleDto>> GetGradeScaleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var gradeScale = await _unitOfWork.GradeScales.GetByIdAsync(id, cancellationToken);
            if (gradeScale == null)
            {
                var notFoundResponse = CommonResponse<GradeScaleDto>.Fail(ResponseCodes.NotFound, "Grade scale with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var gradeScaleDto = GradeScaleMapper.ToDto(gradeScale);
            var successResponse = CommonResponse<GradeScaleDto>.Success(gradeScaleDto);
            return successResponse;
        }

        public async Task<CommonResponse<List<GradeScaleDto>>> GetGradeScalesAsync(CancellationToken cancellationToken = default)
        {
            var gradeScales = await _unitOfWork.GradeScales.GetAllOrderedAsync(cancellationToken);

            var gradeScaleDtos = new List<GradeScaleDto>();
            foreach (var gradeScale in gradeScales)
            {
                var gradeScaleDto = GradeScaleMapper.ToDto(gradeScale);
                gradeScaleDtos.Add(gradeScaleDto);
            }

            var successResponse = CommonResponse<List<GradeScaleDto>>.Success(gradeScaleDtos);
            return successResponse;
        }

        public async Task<CommonResponse<GradeScaleDto>> UpdateGradeScaleAsync(Guid id, UpdateGradeScaleCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<GradeScaleDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var gradeScale = await _unitOfWork.GradeScales.GetByIdAsync(id, cancellationToken);
            if (gradeScale == null)
            {
                var notFoundResponse = CommonResponse<GradeScaleDto>.Fail(ResponseCodes.NotFound, "Grade scale with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            gradeScale.MinPercent = command.MinPercent;
            gradeScale.MaxPercent = command.MaxPercent;
            gradeScale.GradePoint = command.GradePoint;
            gradeScale.Remarks = command.Remarks;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var gradeScaleDto = GradeScaleMapper.ToDto(gradeScale);
            var successResponse = CommonResponse<GradeScaleDto>.Success(gradeScaleDto, "Grade scale updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteGradeScaleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var gradeScale = await _unitOfWork.GradeScales.GetByIdAsync(id, cancellationToken);
            if (gradeScale == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Grade scale with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            _unitOfWork.GradeScales.Remove(gradeScale);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Grade scale deleted successfully.");
            return successResponse;
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var error in validationResult.Errors)
            {
                errorMessages.Add(error.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
