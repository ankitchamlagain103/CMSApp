using Application.Common.Interfaces;
using Application.Common.Models;
using Application.LeaveTypes.Commands;
using Application.LeaveTypes.Dtos;
using Application.LeaveTypes.Validators;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.LeaveTypes
{
    public class LeaveTypeService : ILeaveTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateLeaveTypeCommandValidator _createValidator;
        private readonly UpdateLeaveTypeCommandValidator _updateValidator;

        public LeaveTypeService(
            IUnitOfWork unitOfWork,
            CreateLeaveTypeCommandValidator createValidator,
            UpdateLeaveTypeCommandValidator updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<CommonResponse<LeaveTypeDto>> CreateLeaveTypeAsync(CreateLeaveTypeCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<LeaveTypeDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var name = command.Name.Trim();
            var nameExists = await _unitOfWork.LeaveTypes.NameExistsAsync(name, cancellationToken);
            if (nameExists)
            {
                var conflictResponse = CommonResponse<LeaveTypeDto>.Fail(ResponseCodes.Conflict, "Leave type '" + name + "' already exists (possibly soft-deleted).");
                return conflictResponse;
            }

            var leaveType = new LeaveType
            {
                Name = name,
                DaysPerYear = command.DaysPerYear,
                CarryForward = command.CarryForward,
                IsPaid = command.IsPaid,
                MaxConsecutiveDays = command.MaxConsecutiveDays,
                MaxDaysPerWeek = command.MaxDaysPerWeek,
                MaxDaysPerMonth = command.MaxDaysPerMonth
            };

            await _unitOfWork.LeaveTypes.AddAsync(leaveType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var leaveTypeDto = LeaveTypeMapper.ToDto(leaveType);
            var successResponse = CommonResponse<LeaveTypeDto>.Success(leaveTypeDto, "Leave type created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<LeaveTypeDto>> GetLeaveTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var leaveType = await _unitOfWork.LeaveTypes.GetByIdAsync(id, cancellationToken);
            if (leaveType == null)
            {
                var notFoundResponse = CommonResponse<LeaveTypeDto>.Fail(ResponseCodes.NotFound, "Leave type with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var leaveTypeDto = LeaveTypeMapper.ToDto(leaveType);
            var successResponse = CommonResponse<LeaveTypeDto>.Success(leaveTypeDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<LeaveTypeDto>>> GetLeaveTypesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedLeaveTypes = await _unitOfWork.LeaveTypes.GetPagedAsync(page, pageSize, cancellationToken);

            var leaveTypeDtos = new List<LeaveTypeDto>();
            foreach (var leaveType in pagedLeaveTypes.Items)
            {
                var leaveTypeDto = LeaveTypeMapper.ToDto(leaveType);
                leaveTypeDtos.Add(leaveTypeDto);
            }

            var paginatedResponse = new PaginatedResponse<LeaveTypeDto>
            {
                Items = leaveTypeDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedLeaveTypes.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<LeaveTypeDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<LeaveTypeDto>> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<LeaveTypeDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var leaveType = await _unitOfWork.LeaveTypes.GetByIdAsync(id, cancellationToken);
            if (leaveType == null)
            {
                var notFoundResponse = CommonResponse<LeaveTypeDto>.Fail(ResponseCodes.NotFound, "Leave type with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var name = command.Name.Trim();
            if (name != leaveType.Name)
            {
                var nameExists = await _unitOfWork.LeaveTypes.NameExistsAsync(name, cancellationToken);
                if (nameExists)
                {
                    var conflictResponse = CommonResponse<LeaveTypeDto>.Fail(ResponseCodes.Conflict, "Leave type '" + name + "' already exists (possibly soft-deleted).");
                    return conflictResponse;
                }
            }

            leaveType.Name = name;
            leaveType.DaysPerYear = command.DaysPerYear;
            leaveType.CarryForward = command.CarryForward;
            leaveType.IsPaid = command.IsPaid;
            leaveType.MaxConsecutiveDays = command.MaxConsecutiveDays;
            leaveType.MaxDaysPerWeek = command.MaxDaysPerWeek;
            leaveType.MaxDaysPerMonth = command.MaxDaysPerMonth;

            _unitOfWork.LeaveTypes.Update(leaveType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var leaveTypeDto = LeaveTypeMapper.ToDto(leaveType);
            var successResponse = CommonResponse<LeaveTypeDto>.Success(leaveTypeDto, "Leave type updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteLeaveTypeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var leaveType = await _unitOfWork.LeaveTypes.GetByIdAsync(id, cancellationToken);
            if (leaveType == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Leave type with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var isReferenced = await _unitOfWork.LeaveTypes.IsReferencedAsync(id, cancellationToken);
            if (isReferenced)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This leave type still has balances or leave requests. Remove them first.");
                return conflictResponse;
            }

            _unitOfWork.LeaveTypes.Remove(leaveType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Leave type deleted successfully.");
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
