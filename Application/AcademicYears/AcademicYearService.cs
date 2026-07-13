using Application.AcademicYears.Commands;
using Application.AcademicYears.Dtos;
using Application.AcademicYears.Validators;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.AcademicYears
{
    public class AcademicYearService : IAcademicYearService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateAcademicYearCommandValidator _createValidator;
        private readonly UpdateAcademicYearCommandValidator _updateValidator;
        private readonly CloneYearStructureCommandValidator _cloneValidator;

        public AcademicYearService(
            IUnitOfWork unitOfWork,
            CreateAcademicYearCommandValidator createValidator,
            UpdateAcademicYearCommandValidator updateValidator,
            CloneYearStructureCommandValidator cloneValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _cloneValidator = cloneValidator;
        }

        public async Task<CommonResponse<AcademicYearDto>> CreateAcademicYearAsync(CreateAcademicYearCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AcademicYearDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var trimmedCode = command.Code.Trim();
            var codeAlreadyExists = await _unitOfWork.AcademicYears.CodeExistsAsync(trimmedCode, cancellationToken);
            if (codeAlreadyExists)
            {
                var conflictMessage = "Academic year code '" + trimmedCode + "' is already in use (possibly by a soft-deleted year).";
                var conflictResponse = CommonResponse<AcademicYearDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            if (command.IsCurrent)
            {
                await UnsetCurrentYearsAsync(cancellationToken);
            }

            var academicYear = new AcademicYear
            {
                Code = trimmedCode,
                Name = command.Name.Trim(),
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                IsCurrent = command.IsCurrent,
                Status = RecordStatus.Active
            };

            await _unitOfWork.AcademicYears.AddAsync(academicYear, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var academicYearDto = AcademicYearMapper.ToDto(academicYear);
            var successResponse = CommonResponse<AcademicYearDto>.Success(academicYearDto, "Academic year created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<AcademicYearDto>> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(id, cancellationToken);
            if (academicYear == null)
            {
                var notFoundResponse = CommonResponse<AcademicYearDto>.Fail(ResponseCodes.NotFound, "Academic year with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var academicYearDto = AcademicYearMapper.ToDto(academicYear);
            var successResponse = CommonResponse<AcademicYearDto>.Success(academicYearDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<AcademicYearDto>>> GetAcademicYearsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedYears = await _unitOfWork.AcademicYears.GetPagedOrderedAsync(page, pageSize, cancellationToken);

            var academicYearDtos = new List<AcademicYearDto>();
            foreach (var academicYear in pagedYears.Items)
            {
                var academicYearDto = AcademicYearMapper.ToDto(academicYear);
                academicYearDtos.Add(academicYearDto);
            }

            var paginatedResponse = new PaginatedResponse<AcademicYearDto>
            {
                Items = academicYearDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedYears.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<AcademicYearDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<AcademicYearDto>> UpdateAcademicYearAsync(Guid id, UpdateAcademicYearCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AcademicYearDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(id, cancellationToken);
            if (academicYear == null)
            {
                var notFoundResponse = CommonResponse<AcademicYearDto>.Fail(ResponseCodes.NotFound, "Academic year with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            // Single-current invariant: promoting this year to current demotes every other one.
            var becomingCurrent = command.IsCurrent && !academicYear.IsCurrent;
            if (becomingCurrent)
            {
                await UnsetCurrentYearsAsync(cancellationToken);
            }

            academicYear.Name = command.Name.Trim();
            academicYear.StartDate = command.StartDate;
            academicYear.EndDate = command.EndDate;
            academicYear.IsCurrent = command.IsCurrent;
            academicYear.Status = command.Status;

            _unitOfWork.AcademicYears.Update(academicYear);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var academicYearDto = AcademicYearMapper.ToDto(academicYear);
            var successResponse = CommonResponse<AcademicYearDto>.Success(academicYearDto, "Academic year updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteAcademicYearAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(id, cancellationToken);
            if (academicYear == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Academic year with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var hasClasses = await _unitOfWork.AcademicYears.HasClassesAsync(id, cancellationToken);
            if (hasClasses)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This academic year still has classes. Delete or move them first.");
                return conflictResponse;
            }

            _unitOfWork.AcademicYears.Remove(academicYear);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Academic year deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<CloneStructureResultDto>> CloneStructureAsync(Guid targetAcademicYearId, CloneYearStructureCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _cloneValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<CloneStructureResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            if (command.SourceAcademicYearId == targetAcademicYearId)
            {
                var sameYearResponse = CommonResponse<CloneStructureResultDto>.Fail(ResponseCodes.ValidationError, "Source and target academic years must differ.");
                return sameYearResponse;
            }

            var targetYear = await _unitOfWork.AcademicYears.GetByIdAsync(targetAcademicYearId, cancellationToken);
            if (targetYear == null)
            {
                var targetNotFoundResponse = CommonResponse<CloneStructureResultDto>.Fail(ResponseCodes.NotFound, "Academic year with id '" + targetAcademicYearId + "' was not found.");
                return targetNotFoundResponse;
            }

            var sourceYear = await _unitOfWork.AcademicYears.GetByIdAsync(command.SourceAcademicYearId, cancellationToken);
            if (sourceYear == null)
            {
                var sourceNotFoundResponse = CommonResponse<CloneStructureResultDto>.Fail(ResponseCodes.NotFound, "Source academic year with id '" + command.SourceAcademicYearId + "' was not found.");
                return sourceNotFoundResponse;
            }

            var sourceClasses = await _unitOfWork.AcademicClasses.GetByYearWithChildrenAsync(command.SourceAcademicYearId, cancellationToken);

            var result = new CloneStructureResultDto
            {
                SourceAcademicYearId = command.SourceAcademicYearId,
                TargetAcademicYearId = targetAcademicYearId
            };

            foreach (var sourceClass in sourceClasses)
            {
                // A grade already present in the target year is left alone -- the clone is
                // additive, never a merge into existing classes.
                var gradeAlreadyExists = await _unitOfWork.AcademicClasses.CombinationExistsAsync(targetAcademicYearId, sourceClass.GradeCode, cancellationToken);
                if (gradeAlreadyExists)
                {
                    result.SkippedGradeCodes.Add(sourceClass.GradeCode);
                    continue;
                }

                var newClass = new AcademicClass
                {
                    AcademicYearId = targetAcademicYearId,
                    GradeCode = sourceClass.GradeCode,
                    Status = RecordStatus.Active
                };

                // New sections are keyed by SectionCode so section-scoped subject rows can be
                // remapped onto their clones below.
                var newSectionsByCode = new Dictionary<string, ClassSection>();
                foreach (var sourceSection in sourceClass.Sections)
                {
                    var newSection = new ClassSection
                    {
                        SectionCode = sourceSection.SectionCode,
                        Capacity = sourceSection.Capacity,
                        Status = RecordStatus.Active
                    };

                    newClass.Sections.Add(newSection);
                    newSectionsByCode.Add(newSection.SectionCode, newSection);
                    result.SectionsCreated++;
                }

                foreach (var sourceSubject in sourceClass.ClassSubjects)
                {
                    ClassSection scopedSection = null;
                    if (sourceSubject.ClassSectionId.HasValue)
                    {
                        // The source section may be soft-deleted (nav filtered out) or its clone
                        // missing -- a scoped subject with no home section isn't copied.
                        if (sourceSubject.ClassSection == null || !newSectionsByCode.TryGetValue(sourceSubject.ClassSection.SectionCode, out scopedSection))
                        {
                            continue;
                        }
                    }

                    var newSubject = new ClassSubject
                    {
                        SubjectCode = sourceSubject.SubjectCode,
                        IsMandatory = sourceSubject.IsMandatory,
                        DisplayOrder = sourceSubject.DisplayOrder,
                        ClassSection = scopedSection
                    };

                    newClass.ClassSubjects.Add(newSubject);
                    result.SubjectsCreated++;
                }

                await _unitOfWork.AcademicClasses.AddAsync(newClass, cancellationToken);
                result.ClassesCreated++;
            }

            // One save for the whole clone -- it either lands completely or not at all.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<CloneStructureResultDto>.Success(result, "Structure cloned from year '" + sourceYear.Code + "' successfully.");
            return successResponse;
        }

        private async Task UnsetCurrentYearsAsync(CancellationToken cancellationToken)
        {
            var currentYears = await _unitOfWork.AcademicYears.GetCurrentYearsAsync(cancellationToken);
            foreach (var currentYear in currentYears)
            {
                currentYear.IsCurrent = false;
                _unitOfWork.AcademicYears.Update(currentYear);
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
