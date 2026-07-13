using Application.AcademicClasses.Commands;
using Application.AcademicClasses.Dtos;
using Application.AcademicClasses.Queries;
using Application.AcademicClasses.Validators;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.AcademicClasses
{
    public class AcademicClassService : IAcademicClassService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateAcademicClassCommandValidator _createValidator;
        private readonly UpdateAcademicClassCommandValidator _updateValidator;
        private readonly CreateClassSectionCommandValidator _createSectionValidator;
        private readonly UpdateClassSectionCommandValidator _updateSectionValidator;
        private readonly AssignClassSubjectCommandValidator _assignSubjectValidator;

        public AcademicClassService(
            IUnitOfWork unitOfWork,
            CreateAcademicClassCommandValidator createValidator,
            UpdateAcademicClassCommandValidator updateValidator,
            CreateClassSectionCommandValidator createSectionValidator,
            UpdateClassSectionCommandValidator updateSectionValidator,
            AssignClassSubjectCommandValidator assignSubjectValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _createSectionValidator = createSectionValidator;
            _updateSectionValidator = updateSectionValidator;
            _assignSubjectValidator = assignSubjectValidator;
        }

        public async Task<CommonResponse<AcademicClassDto>> CreateAcademicClassAsync(CreateAcademicClassCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(command.AcademicYearId, cancellationToken);
            if (academicYear == null)
            {
                var yearNotFoundResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.NotFound, "Academic year with id '" + command.AcademicYearId + "' was not found.");
                return yearNotFoundResponse;
            }

            // Grade/Section are Config codes -- validate against the catalog, since there is no
            // database FK backing these columns.
            var gradeCode = command.GradeCode.Trim();
            var gradeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.Grade, gradeCode, cancellationToken);
            if (!gradeExists)
            {
                var gradeInvalidResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.ValidationError, "GradeCode '" + gradeCode + "' is not a known grade option.");
                return gradeInvalidResponse;
            }

            var combinationExists = await _unitOfWork.AcademicClasses.CombinationExistsAsync(command.AcademicYearId, gradeCode, cancellationToken);
            if (combinationExists)
            {
                var conflictResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.Conflict, "A class for this year and grade already exists (possibly soft-deleted). Add sections to it instead.");
                return conflictResponse;
            }

            // Initial sections are optional; when present, validate every section code up front
            // (against the catalog and for in-list duplicates) so the create is all-or-nothing.
            var sectionCodes = new List<string>();
            foreach (var sectionCommand in command.Sections)
            {
                var sectionCode = sectionCommand.SectionCode.Trim();
                if (sectionCodes.Contains(sectionCode))
                {
                    var duplicateSectionResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.ValidationError, "SectionCode '" + sectionCode + "' appears more than once.");
                    return duplicateSectionResponse;
                }

                var sectionExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.Section, sectionCode, cancellationToken);
                if (!sectionExists)
                {
                    var sectionInvalidResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.ValidationError, "SectionCode '" + sectionCode + "' is not a known section option.");
                    return sectionInvalidResponse;
                }

                sectionCodes.Add(sectionCode);
            }

            var academicClass = new AcademicClass
            {
                AcademicYearId = command.AcademicYearId,
                GradeCode = gradeCode,
                Status = RecordStatus.Active
            };

            for (var index = 0; index < command.Sections.Count; index++)
            {
                var section = new ClassSection
                {
                    SectionCode = sectionCodes[index],
                    Capacity = command.Sections[index].Capacity,
                    Status = RecordStatus.Active
                };
                academicClass.Sections.Add(section);
            }

            await _unitOfWork.AcademicClasses.AddAsync(academicClass, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var academicClassDto = AcademicClassMapper.ToDto(academicClass);
            var successResponse = CommonResponse<AcademicClassDto>.Success(academicClassDto, "Class created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<AcademicClassDto>> GetAcademicClassByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var academicClass = await _unitOfWork.AcademicClasses.GetWithSectionsAsync(id, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.NotFound, "Class with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var academicClassDto = AcademicClassMapper.ToDto(academicClass);
            var successResponse = CommonResponse<AcademicClassDto>.Success(academicClassDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<AcademicClassDto>>> GetAcademicClassesAsync(GetAcademicClassesQuery query, CancellationToken cancellationToken = default)
        {
            var pagedClasses = await _unitOfWork.AcademicClasses.GetPagedByFilterAsync(query.AcademicYearId, query.Page, query.PageSize, cancellationToken);

            var academicClassDtos = new List<AcademicClassDto>();
            foreach (var academicClass in pagedClasses.Items)
            {
                var academicClassDto = AcademicClassMapper.ToDto(academicClass);
                academicClassDtos.Add(academicClassDto);
            }

            var paginatedResponse = new PaginatedResponse<AcademicClassDto>
            {
                Items = academicClassDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedClasses.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<AcademicClassDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<AcademicClassDto>> UpdateAcademicClassAsync(Guid id, UpdateAcademicClassCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicClass = await _unitOfWork.AcademicClasses.GetWithSectionsAsync(id, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<AcademicClassDto>.Fail(ResponseCodes.NotFound, "Class with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            academicClass.Status = command.Status;

            _unitOfWork.AcademicClasses.Update(academicClass);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var academicClassDto = AcademicClassMapper.ToDto(academicClass);
            var successResponse = CommonResponse<AcademicClassDto>.Success(academicClassDto, "Class updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteAcademicClassAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(id, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Class with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            // A hidden class would orphan its sections (and their enrollments) -- remove the
            // sections first, which in turn requires their enrollments to be gone.
            var hasSections = await _unitOfWork.AcademicClasses.HasSectionsAsync(id, cancellationToken);
            if (hasSections)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This class still has sections. Remove them first.");
                return conflictResponse;
            }

            _unitOfWork.AcademicClasses.Remove(academicClass);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Class deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ClassSectionDto>> AddSectionAsync(Guid academicClassId, CreateClassSectionCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createSectionValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ClassSectionDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<ClassSectionDto>.Fail(ResponseCodes.NotFound, "Class with id '" + academicClassId + "' was not found.");
                return notFoundResponse;
            }

            var sectionCode = command.SectionCode.Trim();
            var sectionCodeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.Section, sectionCode, cancellationToken);
            if (!sectionCodeExists)
            {
                var sectionInvalidResponse = CommonResponse<ClassSectionDto>.Fail(ResponseCodes.ValidationError, "SectionCode '" + sectionCode + "' is not a known section option.");
                return sectionInvalidResponse;
            }

            var alreadyExists = await _unitOfWork.AcademicClasses.SectionExistsAsync(academicClassId, sectionCode, cancellationToken);
            if (alreadyExists)
            {
                var conflictResponse = CommonResponse<ClassSectionDto>.Fail(ResponseCodes.Conflict, "Section '" + sectionCode + "' already exists on this class (possibly soft-deleted).");
                return conflictResponse;
            }

            var classSection = new ClassSection
            {
                AcademicClassId = academicClassId,
                SectionCode = sectionCode,
                Capacity = command.Capacity,
                Status = RecordStatus.Active
            };

            await _unitOfWork.AcademicClasses.AddSectionAsync(classSection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var classSectionDto = AcademicClassMapper.ToSectionDto(classSection);
            var successResponse = CommonResponse<ClassSectionDto>.Success(classSectionDto, "Section added to class successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ClassSectionDto>> UpdateSectionAsync(Guid academicClassId, Guid classSectionId, UpdateClassSectionCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateSectionValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ClassSectionDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var classSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(classSectionId, cancellationToken);
            if (classSection == null || classSection.AcademicClassId != academicClassId)
            {
                var notFoundResponse = CommonResponse<ClassSectionDto>.Fail(ResponseCodes.NotFound, "Section was not found on this class.");
                return notFoundResponse;
            }

            classSection.Capacity = command.Capacity;
            classSection.Status = command.Status;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var classSectionDto = AcademicClassMapper.ToSectionDto(classSection);
            var successResponse = CommonResponse<ClassSectionDto>.Success(classSectionDto, "Section updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveSectionAsync(Guid academicClassId, Guid classSectionId, CancellationToken cancellationToken = default)
        {
            var classSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(classSectionId, cancellationToken);
            if (classSection == null || classSection.AcademicClassId != academicClassId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Section was not found on this class.");
                return notFoundResponse;
            }

            var hasEnrollments = await _unitOfWork.AcademicClasses.SectionHasEnrollmentsAsync(classSectionId, cancellationToken);
            if (hasEnrollments)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This section still has enrollments. Remove them first.");
                return conflictResponse;
            }

            // Same reasoning as enrollments: a soft-deleted section must not leave teacher
            // assignments pointing at it.
            var hasTeacherAssignments = await _unitOfWork.AcademicClasses.SectionHasTeacherAssignmentsAsync(classSectionId, cancellationToken);
            if (hasTeacherAssignments)
            {
                var assignmentsConflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This section still has teacher assignments. Remove them first.");
                return assignmentsConflictResponse;
            }

            var hasScopedSubjects = await _unitOfWork.AcademicClasses.SectionHasScopedSubjectsAsync(classSectionId, cancellationToken);
            if (hasScopedSubjects)
            {
                var scopedSubjectsConflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This section still has section-scoped subjects. Remove them first.");
                return scopedSubjectsConflictResponse;
            }

            _unitOfWork.AcademicClasses.RemoveSection(classSection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Section removed from class successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<ClassSectionDto>>> GetSectionsAsync(Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<List<ClassSectionDto>>.Fail(ResponseCodes.NotFound, "Class with id '" + academicClassId + "' was not found.");
                return notFoundResponse;
            }

            var sections = await _unitOfWork.AcademicClasses.GetSectionsAsync(academicClassId, cancellationToken);

            var sectionDtos = new List<ClassSectionDto>();
            foreach (var section in sections)
            {
                var sectionDto = AcademicClassMapper.ToSectionDto(section);
                sectionDtos.Add(sectionDto);
            }

            var successResponse = CommonResponse<List<ClassSectionDto>>.Success(sectionDtos);
            return successResponse;
        }

        public async Task<CommonResponse<ClassSubjectDto>> AssignSubjectAsync(Guid academicClassId, AssignClassSubjectCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _assignSubjectValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.NotFound, "Class with id '" + academicClassId + "' was not found.");
                return notFoundResponse;
            }

            var subjectCode = command.SubjectCode.Trim();
            var subjectExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.Subject, subjectCode, cancellationToken);
            if (!subjectExists)
            {
                var subjectInvalidResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.ValidationError, "SubjectCode '" + subjectCode + "' is not a known subject option.");
                return subjectInvalidResponse;
            }

            // Mandatory subjects are always class-wide ("same class, same subjects"); only an
            // optional subject may be scoped to one section.
            ClassSection scopedSection = null;
            if (command.ClassSectionId.HasValue)
            {
                if (command.IsMandatory)
                {
                    var mandatoryScopedResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.ValidationError, "A mandatory subject cannot be scoped to a section -- it applies to the whole class.");
                    return mandatoryScopedResponse;
                }

                scopedSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.ClassSectionId.Value, cancellationToken);
                if (scopedSection == null)
                {
                    var sectionNotFoundResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.NotFound, "Class section with id '" + command.ClassSectionId.Value + "' was not found.");
                    return sectionNotFoundResponse;
                }

                if (scopedSection.AcademicClassId != academicClassId)
                {
                    var sectionMismatchResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.ValidationError, "That section belongs to a different class.");
                    return sectionMismatchResponse;
                }
            }

            // A subject appears either once class-wide or once per section, never both.
            var existingRows = await _unitOfWork.AcademicClasses.GetClassSubjectRowsByCodeAsync(academicClassId, subjectCode, cancellationToken);
            if (command.ClassSectionId.HasValue)
            {
                foreach (var existingRow in existingRows)
                {
                    if (existingRow.ClassSectionId == null)
                    {
                        var classWideConflictResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.Conflict, "Subject '" + subjectCode + "' is already offered class-wide. Remove that row before scoping it to sections.");
                        return classWideConflictResponse;
                    }

                    if (existingRow.ClassSectionId == command.ClassSectionId.Value)
                    {
                        var sameSectionConflictResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.Conflict, "Subject '" + subjectCode + "' is already offered in that section.");
                        return sameSectionConflictResponse;
                    }
                }
            }
            else if (existingRows.Count > 0)
            {
                var conflictResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.Conflict, "Subject '" + subjectCode + "' is already assigned to this class (class-wide or section-scoped).");
                return conflictResponse;
            }

            var classSubject = new ClassSubject
            {
                AcademicClassId = academicClassId,
                ClassSectionId = command.ClassSectionId,
                SubjectCode = subjectCode,
                IsMandatory = command.IsMandatory,
                DisplayOrder = command.DisplayOrder,
                ClassSection = scopedSection
            };

            await _unitOfWork.AcademicClasses.AddClassSubjectAsync(classSubject, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var classSubjectDto = AcademicClassMapper.ToClassSubjectDto(classSubject);
            var successResponse = CommonResponse<ClassSubjectDto>.Success(classSubjectDto, "Subject assigned to class successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveSubjectAsync(Guid academicClassId, Guid classSubjectId, CancellationToken cancellationToken = default)
        {
            var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(classSubjectId, cancellationToken);
            if (classSubject == null || classSubject.AcademicClassId != academicClassId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Class subject was not found on this class.");
                return notFoundResponse;
            }

            // A subject that teachers are assigned to or students have elected can't silently
            // vanish -- those links must be removed first.
            var subjectInUse = await _unitOfWork.AcademicClasses.ClassSubjectInUseAsync(classSubjectId, cancellationToken);
            if (subjectInUse)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This subject has teacher assignments or student electives. Remove those first.");
                return conflictResponse;
            }

            _unitOfWork.AcademicClasses.RemoveClassSubject(classSubject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Subject removed from class successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<ClassSubjectDto>>> GetClassSubjectsAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<List<ClassSubjectDto>>.Fail(ResponseCodes.NotFound, "Class with id '" + academicClassId + "' was not found.");
                return notFoundResponse;
            }

            // classSectionId narrows to one section's effective list (class-wide rows plus that
            // section's scoped rows) -- the shape the enrollment elective picker needs.
            var classSubjects = await _unitOfWork.AcademicClasses.GetClassSubjectsAsync(academicClassId, classSectionId, cancellationToken);

            var classSubjectDtos = new List<ClassSubjectDto>();
            foreach (var classSubject in classSubjects)
            {
                var classSubjectDto = AcademicClassMapper.ToClassSubjectDto(classSubject);
                classSubjectDtos.Add(classSubjectDto);
            }

            var successResponse = CommonResponse<List<ClassSubjectDto>>.Success(classSubjectDtos);
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
