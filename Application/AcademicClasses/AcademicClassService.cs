using Application.AcademicClasses.Commands;
using Application.AcademicClasses.Dtos;
using Application.AcademicClasses.Queries;
using Application.AcademicClasses.Validators;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Teachers;
using Application.Teachers.Dtos;
using Domain.Common.Filters;
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
        private readonly UpdateClassSubjectCommandValidator _updateSubjectValidator;

        public AcademicClassService(
            IUnitOfWork unitOfWork,
            CreateAcademicClassCommandValidator createValidator,
            UpdateAcademicClassCommandValidator updateValidator,
            CreateClassSectionCommandValidator createSectionValidator,
            UpdateClassSectionCommandValidator updateSectionValidator,
            AssignClassSubjectCommandValidator assignSubjectValidator,
            UpdateClassSubjectCommandValidator updateSubjectValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _createSectionValidator = createSectionValidator;
            _updateSectionValidator = updateSectionValidator;
            _assignSubjectValidator = assignSubjectValidator;
            _updateSubjectValidator = updateSubjectValidator;
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
                Order = command.Order,
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
            var filter = new AcademicClassFilter
            {
                AcademicYearId = query.AcademicYearId,
                GradeCode = query.GradeCode,
                Status = query.Status
            };

            var pagedClasses = await _unitOfWork.AcademicClasses.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

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

            academicClass.Order = command.Order;
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

            var compositeMarks = ResolveCompositeMarks(command.HasTheory, command.HasPractical, command.TheoryMarks, command.TheoryPassMarks, command.PracticalMarks, command.PracticalPassMarks);

            var classSubject = new ClassSubject
            {
                AcademicClassId = academicClassId,
                ClassSectionId = command.ClassSectionId,
                SubjectCode = subjectCode,
                IsMandatory = command.IsMandatory,
                DisplayOrder = command.DisplayOrder,
                ClassSection = scopedSection,
                CreditHours = command.CreditHours,
                FullMarks = compositeMarks.FullMarks,
                PassMarks = compositeMarks.PassMarks,
                TheoryMarks = compositeMarks.TheoryMarks,
                PracticalMarks = compositeMarks.PracticalMarks,
                HasTheory = command.HasTheory,
                HasPractical = command.HasPractical,
                TheoryPassMarks = compositeMarks.TheoryPassMarks,
                PracticalPassMarks = compositeMarks.PracticalPassMarks
            };

            await _unitOfWork.AcademicClasses.AddClassSubjectAsync(classSubject, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var classSubjectDto = AcademicClassMapper.ToClassSubjectDto(classSubject);
            var successResponse = CommonResponse<ClassSubjectDto>.Success(classSubjectDto, "Subject assigned to class successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ClassSubjectDto>> UpdateSubjectAsync(Guid academicClassId, Guid classSubjectId, UpdateClassSubjectCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateSubjectValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(classSubjectId, cancellationToken);
            if (classSubject == null || classSubject.AcademicClassId != academicClassId)
            {
                var notFoundResponse = CommonResponse<ClassSubjectDto>.Fail(ResponseCodes.NotFound, "Class subject was not found on this class.");
                return notFoundResponse;
            }

            var compositeMarks = ResolveCompositeMarks(command.HasTheory, command.HasPractical, command.TheoryMarks, command.TheoryPassMarks, command.PracticalMarks, command.PracticalPassMarks);

            // SubjectCode/IsMandatory/ClassSectionId are identity-like and stay immutable -- only
            // grading metadata and display order can change here.
            classSubject.DisplayOrder = command.DisplayOrder;
            classSubject.CreditHours = command.CreditHours;
            classSubject.FullMarks = compositeMarks.FullMarks;
            classSubject.PassMarks = compositeMarks.PassMarks;
            classSubject.TheoryMarks = compositeMarks.TheoryMarks;
            classSubject.PracticalMarks = compositeMarks.PracticalMarks;
            classSubject.HasTheory = command.HasTheory;
            classSubject.HasPractical = command.HasPractical;
            classSubject.TheoryPassMarks = compositeMarks.TheoryPassMarks;
            classSubject.PracticalPassMarks = compositeMarks.PracticalPassMarks;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var classSubjectDto = AcademicClassMapper.ToClassSubjectDto(classSubject);
            var successResponse = CommonResponse<ClassSubjectDto>.Success(classSubjectDto, "Class subject updated successfully.");
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

        // Class-scoped counterpart to ITeacherService.AssignClassSubjectBulkEntryAsync -- that one
        // is scoped to one teacher (route id) and lets each row name its own class/subject/
        // section/period; this one is scoped to one AcademicClass (route id) and lets each row
        // name its own TeacherId, so an admin working from a class's page can map every teacher
        // who teaches that class -- across its subjects, sections and time periods -- in one
        // submission instead of visiting each teacher's profile in turn. Reuses
        // TeacherAssignmentBuilder.BuildAsync, the exact same per-row validation
        // ITeacherService's own assignment endpoints run, so a row succeeds or fails identically
        // regardless of which side of the relationship it was submitted from.
        public async Task<CommonResponse<ClassTeacherAssignmentBulkEntryResultDto>> AssignTeachersBulkEntryAsync(Guid academicClassId, AssignClassTeachersBulkEntryCommand command, CancellationToken cancellationToken = default)
        {
            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<ClassTeacherAssignmentBulkEntryResultDto>.Fail(ResponseCodes.NotFound, "Class with id '" + academicClassId + "' was not found.");
                return notFoundResponse;
            }

            if (command.Items == null || command.Items.Count == 0)
            {
                var noItemsResponse = CommonResponse<ClassTeacherAssignmentBulkEntryResultDto>.Fail(ResponseCodes.ValidationError, "At least one item is required.");
                return noItemsResponse;
            }

            var created = new List<TeacherAssignmentDto>();
            var skipped = new List<ClassTeacherAssignmentEntrySkipDto>();
            var teacherCache = new Dictionary<Guid, Teacher>();
            var classSubjectCache = new Dictionary<Guid, ClassSubject>();

            // Tracks what's already been staged earlier in this same request, same reasoning as
            // AssignClassSubjectBulkEntryAsync -- nothing is saved until the loop finishes, so two
            // colliding rows in the same batch would otherwise both pass the database-existence
            // checks inside TeacherAssignmentBuilder.BuildAsync. TeacherId is part of the key here
            // (unlike the teacher-scoped endpoint, where it's constant for the whole request) --
            // the same subject/section pair can validly be taught by two different teachers, only
            // the same teacher assigned to it twice is a duplicate.
            var stagedKeys = new HashSet<(Guid TeacherId, Guid ClassSubjectId, Guid ClassSectionKey)>();
            var stagedClassTeacherSections = new HashSet<Guid>();

            // Same reasoning as stagedKeys above -- TeacherHasTimePeriodConflictAsync (called
            // from TeacherAssignmentBuilder.BuildAsync) only sees rows already committed to the
            // database. Keyed by (TeacherId, TimePeriodId), unlike the teacher-scoped endpoint's
            // version of this guard (where TeacherId is implicit/constant) -- two DIFFERENT
            // teachers can share the same period without conflict, only the SAME teacher named
            // twice for one period in this batch is the problem.
            var stagedTeacherTimePeriods = new HashSet<(Guid TeacherId, Guid TimePeriodId)>();

            for (var itemIndex = 0; itemIndex < command.Items.Count; itemIndex++)
            {
                var item = command.Items[itemIndex];

                if (item.TeacherId == Guid.Empty || item.ClassSubjectId == Guid.Empty)
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "TeacherId and ClassSubjectId are required."
                    };
                    skipped.Add(skip);
                    continue;
                }

                if (!teacherCache.TryGetValue(item.TeacherId, out var teacher))
                {
                    teacher = await _unitOfWork.Teachers.GetByIdAsync(item.TeacherId, cancellationToken);
                    teacherCache[item.TeacherId] = teacher;
                }

                if (teacher == null)
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Teacher with id '" + item.TeacherId + "' was not found."
                    };
                    skipped.Add(skip);
                    continue;
                }

                if (!classSubjectCache.TryGetValue(item.ClassSubjectId, out var classSubject))
                {
                    classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(item.ClassSubjectId, cancellationToken);
                    classSubjectCache[item.ClassSubjectId] = classSubject;
                }

                if (classSubject == null)
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Class subject with id '" + item.ClassSubjectId + "' was not found."
                    };
                    skipped.Add(skip);
                    continue;
                }

                // Scope guard unique to this class-scoped entry point -- ITeacherService's own
                // bulk-entry endpoint has no "this must belong to a specific class" constraint,
                // but this one is reached from one class's own page, so a row naming a subject
                // that belongs to a different class is a request mistake, not a valid cross-class
                // assignment.
                if (classSubject.AcademicClassId != academicClassId)
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "That class subject does not belong to this academic class."
                    };
                    skipped.Add(skip);
                    continue;
                }

                if (item.TimePeriodId.HasValue && stagedTeacherTimePeriods.Contains((item.TeacherId, item.TimePeriodId.Value)))
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Another item earlier in this same request already assigns this teacher to that time period."
                    };
                    skipped.Add(skip);
                    continue;
                }

                var (assignment, errorCode, errorMessage) = await TeacherAssignmentBuilder.BuildAsync(_unitOfWork, item.TeacherId, classSubject, item.ClassSectionId, item.IsClassTeacher, item.TimePeriodId, cancellationToken);
                if (errorCode != null)
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = errorMessage
                    };
                    skipped.Add(skip);
                    continue;
                }

                var sectionKey = assignment.ClassSectionId.HasValue ? assignment.ClassSectionId.Value : Guid.Empty;
                var stagedKey = (item.TeacherId, assignment.ClassSubjectId, sectionKey);
                if (stagedKeys.Contains(stagedKey))
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Duplicate of an earlier item in this same request."
                    };
                    skipped.Add(skip);
                    continue;
                }

                // Unlike stagedKeys, this guard is NOT keyed by TeacherId -- a section has at
                // most one class teacher regardless of who, so two rows naming different
                // teachers as class teacher of the same section conflict just as much as two
                // rows naming the same teacher would.
                if (assignment.IsClassTeacher && stagedClassTeacherSections.Contains(assignment.ClassSectionId.Value))
                {
                    var skip = new ClassTeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        TeacherId = item.TeacherId,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Another item earlier in this same request already makes a teacher the class teacher for that section."
                    };
                    skipped.Add(skip);
                    continue;
                }

                if (assignment.IsClassTeacher)
                {
                    stagedClassTeacherSections.Add(assignment.ClassSectionId.Value);
                }

                if (assignment.TimePeriodId.HasValue)
                {
                    stagedTeacherTimePeriods.Add((item.TeacherId, assignment.TimePeriodId.Value));
                }

                stagedKeys.Add(stagedKey);
                await _unitOfWork.Teachers.AddAssignmentAsync(assignment, cancellationToken);
                created.Add(TeacherMapper.ToAssignmentDto(assignment));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = new ClassTeacherAssignmentBulkEntryResultDto
            {
                Created = created,
                Skipped = skipped
            };
            var successResponse = CommonResponse<ClassTeacherAssignmentBulkEntryResultDto>.Success(resultDto, created.Count + " assignment(s) created, " + skipped.Count + " skipped.");
            return successResponse;
        }

        // Read-side counterpart to AssignTeachersBulkEntryAsync -- "who teaches this class,"
        // listing every existing TeacherAssignment row for the class (optionally narrowed to one
        // section) instead of creating new ones. There was previously no GET endpoint for this --
        // only the bulk-create POST existed.
        public async Task<CommonResponse<List<ClassTeacherAssignmentDto>>> GetTeacherAssignmentsAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(academicClassId, cancellationToken);
            if (academicClass == null)
            {
                var notFoundResponse = CommonResponse<List<ClassTeacherAssignmentDto>>.Fail(ResponseCodes.NotFound, "Class with id '" + academicClassId + "' was not found.");
                return notFoundResponse;
            }

            if (classSectionId.HasValue)
            {
                var classSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(classSectionId.Value, cancellationToken);
                if (classSection == null || classSection.AcademicClassId != academicClassId)
                {
                    var sectionNotFoundResponse = CommonResponse<List<ClassTeacherAssignmentDto>>.Fail(ResponseCodes.NotFound, "Section was not found on this class.");
                    return sectionNotFoundResponse;
                }
            }

            var assignments = await _unitOfWork.Teachers.GetAssignmentsByAcademicClassAsync(academicClassId, classSectionId, cancellationToken);

            var assignmentDtos = new List<ClassTeacherAssignmentDto>();
            foreach (var assignment in assignments)
            {
                var assignmentDto = AcademicClassMapper.ToTeacherAssignmentDto(assignment);
                assignmentDtos.Add(assignmentDto);
            }

            var successListResponse = CommonResponse<List<ClassTeacherAssignmentDto>>.Success(assignmentDtos);
            return successListResponse;
        }

        // Two-tier composite mark structure (2026-07-30): FullMarks/PassMarks are no longer
        // caller-supplied -- they're computed here as TheoryMarks+PracticalMarks /
        // TheoryPassMarks+PracticalPassMarks, so they can never drift from the component figures
        // that actually define them (this also means PassMarks <= FullMarks no longer needs its
        // own validator check -- TheoryPassMarks <= TheoryMarks and PracticalPassMarks <=
        // PracticalMarks together already guarantee it, since summing two component-wise
        // inequalities preserves the total inequality).
        //
        // Theory-only fallback rule (and its practical-only mirror): a disabled component's
        // Marks/PassMarks are forced to 0 -- not left null -- so the sum always reduces to
        // exactly the enabled component's own figures (e.g. HasPractical = false gives
        // PracticalMarks = PracticalPassMarks = 0, so FullMarks = TheoryMarks + 0 = TheoryMarks).
        // A component's sum is only computed once BOTH sides have a value -- if the enabled
        // component itself hasn't been graded yet, the total stays null ("not configured yet"),
        // never a partial/misleading number.
        private static (int? FullMarks, int? PassMarks, int? TheoryMarks, int? TheoryPassMarks, int? PracticalMarks, int? PracticalPassMarks) ResolveCompositeMarks(
            bool hasTheory,
            bool hasPractical,
            int? theoryMarks,
            int? theoryPassMarks,
            int? practicalMarks,
            int? practicalPassMarks)
        {
            var resolvedTheoryMarks = hasTheory ? theoryMarks : 0;
            var resolvedTheoryPassMarks = hasTheory ? theoryPassMarks : 0;
            var resolvedPracticalMarks = hasPractical ? practicalMarks : 0;
            var resolvedPracticalPassMarks = hasPractical ? practicalPassMarks : 0;

            int? fullMarks = null;
            if (resolvedTheoryMarks.HasValue && resolvedPracticalMarks.HasValue)
            {
                fullMarks = resolvedTheoryMarks.Value + resolvedPracticalMarks.Value;
            }

            int? passMarks = null;
            if (resolvedTheoryPassMarks.HasValue && resolvedPracticalPassMarks.HasValue)
            {
                passMarks = resolvedTheoryPassMarks.Value + resolvedPracticalPassMarks.Value;
            }

            return (fullMarks, passMarks, resolvedTheoryMarks, resolvedTheoryPassMarks, resolvedPracticalMarks, resolvedPracticalPassMarks);
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
