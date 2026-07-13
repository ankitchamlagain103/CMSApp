using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Enrollments.Commands;
using Application.Enrollments.Dtos;
using Application.Enrollments.Queries;
using Application.Enrollments.Validators;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Enrollments
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateEnrollmentCommandValidator _createValidator;
        private readonly UpdateEnrollmentCommandValidator _updateValidator;

        public EnrollmentService(
            IUnitOfWork unitOfWork,
            CreateEnrollmentCommandValidator createValidator,
            UpdateEnrollmentCommandValidator updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<CommonResponse<EnrollmentDto>> CreateEnrollmentAsync(CreateEnrollmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var student = await _unitOfWork.Students.GetByIdAsync(command.StudentId, cancellationToken);
            if (student == null)
            {
                var studentNotFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Student with id '" + command.StudentId + "' was not found.");
                return studentNotFoundResponse;
            }

            // The section carries its AcademicClass, which carries the academic year -- both are
            // needed for the one-active-enrollment-per-year invariant below.
            var classSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.ClassSectionId, cancellationToken);
            if (classSection == null)
            {
                var sectionNotFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Class section with id '" + command.ClassSectionId + "' was not found.");
                return sectionNotFoundResponse;
            }

            var enrollmentExists = await _unitOfWork.Enrollments.EnrollmentExistsAsync(command.StudentId, command.ClassSectionId, cancellationToken);
            if (enrollmentExists)
            {
                var duplicateResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This student is already enrolled in that section (possibly a soft-deleted enrollment).");
                return duplicateResponse;
            }

            // A student can hold only one active (Enrolled) enrollment per academic year --
            // being in two grades/sections of the same year at once is a data error. Moving a
            // student means closing the old enrollment (Transferred/Withdrawn) first.
            var academicYearId = classSection.AcademicClass.AcademicYearId;
            var hasActiveEnrollment = await _unitOfWork.Enrollments.HasActiveEnrollmentInYearAsync(command.StudentId, academicYearId, null, cancellationToken);
            if (hasActiveEnrollment)
            {
                var activeConflictResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This student already has an active enrollment in this academic year. Close it (Transferred/Withdrawn/Completed) before enrolling again.");
                return activeConflictResponse;
            }

            // Capacity 0 means unlimited; otherwise only actively enrolled students count toward it.
            if (classSection.Capacity > 0)
            {
                var activeEnrollmentCount = await _unitOfWork.Enrollments.CountActiveBySectionAsync(command.ClassSectionId, cancellationToken);
                if (activeEnrollmentCount >= classSection.Capacity)
                {
                    var capacityResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This section is at full capacity (" + classSection.Capacity + ").");
                    return capacityResponse;
                }
            }

            var trimmedRollNumber = command.RollNumber?.Trim();
            if (!string.IsNullOrEmpty(trimmedRollNumber))
            {
                var rollNumberTaken = await _unitOfWork.Enrollments.RollNumberExistsInSectionAsync(command.ClassSectionId, trimmedRollNumber, null, cancellationToken);
                if (rollNumberTaken)
                {
                    var rollNumberResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "Roll number '" + trimmedRollNumber + "' is already taken in this section.");
                    return rollNumberResponse;
                }
            }

            var enrollment = new Enrollment
            {
                StudentId = command.StudentId,
                ClassSectionId = command.ClassSectionId,
                RollNumber = trimmedRollNumber,
                EnrollmentDate = command.EnrollmentDate,
                Status = EnrollmentStatus.Enrolled,
                Student = student,
                ClassSection = classSection
            };

            await _unitOfWork.Enrollments.AddAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
            var successResponse = CommonResponse<EnrollmentDto>.Success(enrollmentDto, "Student enrolled successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentDto>> GetEnrollmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(id, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
            var successResponse = CommonResponse<EnrollmentDto>.Success(enrollmentDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<EnrollmentDto>>> GetEnrollmentsAsync(GetEnrollmentsQuery query, CancellationToken cancellationToken = default)
        {
            var pagedEnrollments = await _unitOfWork.Enrollments.GetPagedByFilterAsync(query.StudentId, query.AcademicClassId, query.ClassSectionId, query.Page, query.PageSize, cancellationToken);

            var enrollmentDtos = new List<EnrollmentDto>();
            foreach (var enrollment in pagedEnrollments.Items)
            {
                var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
                enrollmentDtos.Add(enrollmentDto);
            }

            var paginatedResponse = new PaginatedResponse<EnrollmentDto>
            {
                Items = enrollmentDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedEnrollments.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<EnrollmentDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentDto>> UpdateEnrollmentAsync(Guid id, UpdateEnrollmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(id, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var trimmedRollNumber = command.RollNumber?.Trim();
            var rollNumberIsChanging = !string.Equals(enrollment.RollNumber, trimmedRollNumber, StringComparison.Ordinal);
            if (rollNumberIsChanging && !string.IsNullOrEmpty(trimmedRollNumber))
            {
                var rollNumberTaken = await _unitOfWork.Enrollments.RollNumberExistsInSectionAsync(enrollment.ClassSectionId, trimmedRollNumber, id, cancellationToken);
                if (rollNumberTaken)
                {
                    var rollNumberResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "Roll number '" + trimmedRollNumber + "' is already taken in this section.");
                    return rollNumberResponse;
                }
            }

            // Reactivating a closed enrollment re-runs the one-active-per-year check -- otherwise
            // flipping an old row back to Enrolled would sneak a second active enrollment in.
            var isReactivating = command.Status == EnrollmentStatus.Enrolled && enrollment.Status != EnrollmentStatus.Enrolled;
            if (isReactivating)
            {
                var academicYearId = enrollment.ClassSection.AcademicClass.AcademicYearId;
                var hasActiveEnrollment = await _unitOfWork.Enrollments.HasActiveEnrollmentInYearAsync(enrollment.StudentId, academicYearId, id, cancellationToken);
                if (hasActiveEnrollment)
                {
                    var activeConflictResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This student already has an active enrollment in this academic year.");
                    return activeConflictResponse;
                }
            }

            enrollment.RollNumber = trimmedRollNumber;
            enrollment.EnrollmentDate = command.EnrollmentDate;
            enrollment.Status = command.Status;

            _unitOfWork.Enrollments.Update(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
            var successResponse = CommonResponse<EnrollmentDto>.Success(enrollmentDto, "Enrollment updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteEnrollmentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(id, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            _unitOfWork.Enrollments.Remove(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Enrollment deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentSubjectDto>> AddElectiveSubjectAsync(Guid enrollmentId, Guid classSubjectId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(classSubjectId, cancellationToken);
            if (classSubject == null)
            {
                var subjectNotFoundResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.NotFound, "Class subject with id '" + classSubjectId + "' was not found.");
                return subjectNotFoundResponse;
            }

            // The elective must be offered by the class the student's section belongs to --
            // subjects hang off the class (grade), so every section shares one subject list.
            if (classSubject.AcademicClassId != enrollment.ClassSection.AcademicClassId)
            {
                var wrongClassResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.ValidationError, "That subject belongs to a different class than this enrollment.");
                return wrongClassResponse;
            }

            // A section-scoped optional subject is only pickable by students of that section.
            if (classSubject.ClassSectionId.HasValue && classSubject.ClassSectionId.Value != enrollment.ClassSectionId)
            {
                var wrongSectionResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.ValidationError, "Subject '" + classSubject.SubjectCode + "' is only offered in a different section.");
                return wrongSectionResponse;
            }

            // Mandatory subjects apply implicitly -- only electives get an explicit row.
            if (classSubject.IsMandatory)
            {
                var mandatoryResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.ValidationError, "Subject '" + classSubject.SubjectCode + "' is mandatory and applies automatically.");
                return mandatoryResponse;
            }

            var alreadyAdded = await _unitOfWork.Enrollments.ElectiveSubjectExistsAsync(enrollmentId, classSubjectId, cancellationToken);
            if (alreadyAdded)
            {
                var conflictResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.Conflict, "This elective is already added to the enrollment.");
                return conflictResponse;
            }

            var electiveSubject = new EnrollmentSubject
            {
                EnrollmentId = enrollmentId,
                ClassSubjectId = classSubjectId,
                ClassSubject = classSubject
            };

            await _unitOfWork.Enrollments.AddElectiveSubjectAsync(electiveSubject, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var electiveSubjectDto = EnrollmentMapper.ToElectiveSubjectDto(electiveSubject);
            var successResponse = CommonResponse<EnrollmentSubjectDto>.Success(electiveSubjectDto, "Elective subject added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveElectiveSubjectAsync(Guid enrollmentId, Guid electiveSubjectId, CancellationToken cancellationToken = default)
        {
            var electiveSubject = await _unitOfWork.Enrollments.GetElectiveSubjectByIdAsync(electiveSubjectId, cancellationToken);
            if (electiveSubject == null || electiveSubject.EnrollmentId != enrollmentId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Elective subject was not found on this enrollment.");
                return notFoundResponse;
            }

            _unitOfWork.Enrollments.RemoveElectiveSubject(electiveSubject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Elective subject removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<EnrollmentSubjectDto>>> GetElectiveSubjectsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<List<EnrollmentSubjectDto>>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var electiveSubjects = await _unitOfWork.Enrollments.GetElectiveSubjectsAsync(enrollmentId, cancellationToken);

            var electiveSubjectDtos = new List<EnrollmentSubjectDto>();
            foreach (var electiveSubject in electiveSubjects)
            {
                var electiveSubjectDto = EnrollmentMapper.ToElectiveSubjectDto(electiveSubject);
                electiveSubjectDtos.Add(electiveSubjectDto);
            }

            var successResponse = CommonResponse<List<EnrollmentSubjectDto>>.Success(electiveSubjectDtos);
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
