using Application.Common.Interfaces;
using Application.Common.Models;
using Application.TimePeriods.Commands;
using Application.TimePeriods.Dtos;
using Application.TimePeriods.Validators;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.TimePeriods
{
    public class TimePeriodService : ITimePeriodService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateTimePeriodCommandValidator _createValidator;
        private readonly UpdateTimePeriodCommandValidator _updateValidator;
        private readonly MapClassTimePeriodsCommandValidator _mapValidator;

        public TimePeriodService(
            IUnitOfWork unitOfWork,
            CreateTimePeriodCommandValidator createValidator,
            UpdateTimePeriodCommandValidator updateValidator,
            MapClassTimePeriodsCommandValidator mapValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _mapValidator = mapValidator;
        }

        public async Task<CommonResponse<TimePeriodDto>> CreateTimePeriodAsync(CreateTimePeriodCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TimePeriodDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var name = command.Name.Trim();
            var nameExists = await _unitOfWork.TimePeriods.NameExistsAsync(name, cancellationToken);
            if (nameExists)
            {
                var conflictResponse = CommonResponse<TimePeriodDto>.Fail(ResponseCodes.Conflict, "Time period '" + name + "' already exists (possibly soft-deleted).");
                return conflictResponse;
            }

            var timePeriod = new TimePeriod
            {
                Name = name,
                StartTime = command.StartTime,
                EndTime = command.EndTime,
                Kind = command.Kind,
                Order = command.Order
            };

            await _unitOfWork.TimePeriods.AddAsync(timePeriod, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var timePeriodDto = TimePeriodMapper.ToDto(timePeriod);
            var successResponse = CommonResponse<TimePeriodDto>.Success(timePeriodDto, "Time period created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<TimePeriodDto>> GetTimePeriodByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var timePeriod = await _unitOfWork.TimePeriods.GetByIdAsync(id, cancellationToken);
            if (timePeriod == null)
            {
                var notFoundResponse = CommonResponse<TimePeriodDto>.Fail(ResponseCodes.NotFound, "Time period with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var timePeriodDto = TimePeriodMapper.ToDto(timePeriod);
            var successResponse = CommonResponse<TimePeriodDto>.Success(timePeriodDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<TimePeriodDto>>> GetTimePeriodsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedTimePeriods = await _unitOfWork.TimePeriods.GetPagedAsync(page, pageSize, cancellationToken);

            var timePeriodDtos = new List<TimePeriodDto>();
            foreach (var timePeriod in pagedTimePeriods.Items)
            {
                var timePeriodDto = TimePeriodMapper.ToDto(timePeriod);
                timePeriodDtos.Add(timePeriodDto);
            }

            var paginatedResponse = new PaginatedResponse<TimePeriodDto>
            {
                Items = timePeriodDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedTimePeriods.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<TimePeriodDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<TimePeriodDto>> UpdateTimePeriodAsync(Guid id, UpdateTimePeriodCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TimePeriodDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var timePeriod = await _unitOfWork.TimePeriods.GetByIdAsync(id, cancellationToken);
            if (timePeriod == null)
            {
                var notFoundResponse = CommonResponse<TimePeriodDto>.Fail(ResponseCodes.NotFound, "Time period with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var name = command.Name.Trim();
            if (name != timePeriod.Name)
            {
                var nameExists = await _unitOfWork.TimePeriods.NameExistsAsync(name, cancellationToken);
                if (nameExists)
                {
                    var conflictResponse = CommonResponse<TimePeriodDto>.Fail(ResponseCodes.Conflict, "Time period '" + name + "' already exists (possibly soft-deleted).");
                    return conflictResponse;
                }
            }

            timePeriod.Name = name;
            timePeriod.StartTime = command.StartTime;
            timePeriod.EndTime = command.EndTime;
            timePeriod.Kind = command.Kind;
            timePeriod.Order = command.Order;

            _unitOfWork.TimePeriods.Update(timePeriod);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var timePeriodDto = TimePeriodMapper.ToDto(timePeriod);
            var successResponse = CommonResponse<TimePeriodDto>.Success(timePeriodDto, "Time period updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteTimePeriodAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var timePeriod = await _unitOfWork.TimePeriods.GetByIdAsync(id, cancellationToken);
            if (timePeriod == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Time period with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var isReferenced = await _unitOfWork.TimePeriods.IsReferencedAsync(id, cancellationToken);
            if (isReferenced)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This time period is still mapped to a class, or referenced by an exam or teacher assignment. Remove those first.");
                return conflictResponse;
            }

            _unitOfWork.TimePeriods.Remove(timePeriod);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Time period deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ClassTimePeriodMapResultDto>> MapClassTimePeriodsAsync(MapClassTimePeriodsCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _mapValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ClassTimePeriodMapResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var distinctClassIds = command.AcademicClassIds.Distinct().ToList();
            var distinctPeriodIds = command.TimePeriodIds.Distinct().ToList();

            var classesById = new Dictionary<Guid, AcademicClass>();
            foreach (var classId in distinctClassIds)
            {
                var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(classId, cancellationToken);
                classesById[classId] = academicClass;
            }

            var periodsById = new Dictionary<Guid, TimePeriod>();
            foreach (var periodId in distinctPeriodIds)
            {
                var timePeriod = await _unitOfWork.TimePeriods.GetByIdAsync(periodId, cancellationToken);
                periodsById[periodId] = timePeriod;
            }

            var created = new List<ClassTimePeriodDto>();
            var skipped = new List<ClassTimePeriodSkipDto>();

            foreach (var classId in distinctClassIds)
            {
                var academicClass = classesById[classId];
                foreach (var periodId in distinctPeriodIds)
                {
                    var timePeriod = periodsById[periodId];

                    if (academicClass == null)
                    {
                        skipped.Add(new ClassTimePeriodSkipDto { AcademicClassId = classId, TimePeriodId = periodId, Reason = "Class with id '" + classId + "' was not found." });
                        continue;
                    }

                    if (timePeriod == null)
                    {
                        skipped.Add(new ClassTimePeriodSkipDto { AcademicClassId = classId, TimePeriodId = periodId, Reason = "Time period with id '" + periodId + "' was not found." });
                        continue;
                    }

                    var alreadyMapped = await _unitOfWork.TimePeriods.IsMappedToClassAsync(classId, periodId, cancellationToken);
                    if (alreadyMapped)
                    {
                        skipped.Add(new ClassTimePeriodSkipDto { AcademicClassId = classId, TimePeriodId = periodId, Reason = "Already mapped." });
                        continue;
                    }

                    var mapping = new ClassTimePeriod
                    {
                        AcademicClassId = classId,
                        TimePeriodId = periodId,
                        AcademicClass = academicClass,
                        TimePeriod = timePeriod
                    };

                    await _unitOfWork.TimePeriods.AddMappingAsync(mapping, cancellationToken);
                    created.Add(TimePeriodMapper.ToClassTimePeriodDto(mapping));
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = new ClassTimePeriodMapResultDto
            {
                Created = created,
                Skipped = skipped
            };
            var successResponse = CommonResponse<ClassTimePeriodMapResultDto>.Success(resultDto, created.Count + " mapping(s) created, " + skipped.Count + " skipped.");
            return successResponse;
        }

        public async Task<CommonResponse<List<ClassTimePeriodDto>>> GetClassTimePeriodsAsync(Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<List<ClassTimePeriodDto>>.Fail(ResponseCodes.NotFound, "Class with id '" + academicClassId + "' was not found.");
                return notFoundResponse;
            }

            var mappings = await _unitOfWork.TimePeriods.GetMappingsByClassIdAsync(academicClassId, cancellationToken);

            var classTimePeriodDtos = new List<ClassTimePeriodDto>();
            foreach (var mapping in mappings)
            {
                var classTimePeriodDto = TimePeriodMapper.ToClassTimePeriodDto(mapping);
                classTimePeriodDtos.Add(classTimePeriodDto);
            }

            var successResponse = CommonResponse<List<ClassTimePeriodDto>>.Success(classTimePeriodDtos);
            return successResponse;
        }

        public async Task<CommonResponse<bool>> UnmapClassTimePeriodAsync(Guid academicClassId, Guid timePeriodId, CancellationToken cancellationToken = default)
        {
            var mapping = await _unitOfWork.TimePeriods.GetMappingAsync(academicClassId, timePeriodId, cancellationToken);
            if (mapping == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "This class is not mapped to that time period.");
                return notFoundResponse;
            }

            _unitOfWork.TimePeriods.RemoveMapping(mapping);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Mapping removed successfully.");
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
