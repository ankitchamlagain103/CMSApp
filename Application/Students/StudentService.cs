using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Students.Commands;
using Application.Students.Dtos;
using Application.Students.Queries;
using Application.Students.Validators;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Students
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateStudentCommandValidator _createValidator;
        private readonly UpdateStudentCommandValidator _updateValidator;
        private readonly LinkGuardianCommandValidator _linkGuardianValidator;

        public StudentService(
            IUnitOfWork unitOfWork,
            CreateStudentCommandValidator createValidator,
            UpdateStudentCommandValidator updateValidator,
            LinkGuardianCommandValidator linkGuardianValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _linkGuardianValidator = linkGuardianValidator;
        }

        public async Task<CommonResponse<StudentDto>> CreateStudentAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var trimmedAdmissionNo = command.AdmissionNo.Trim();
            var admissionNoExists = await _unitOfWork.Students.AdmissionNoExistsAsync(trimmedAdmissionNo, cancellationToken);
            if (admissionNoExists)
            {
                var conflictResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.Conflict, "Admission number '" + trimmedAdmissionNo + "' is already in use (possibly by a soft-deleted student).");
                return conflictResponse;
            }

            // Guardians come with onboarding: resolve every entry (existing guardian by id, or a
            // new Guardian built from the inline fields) BEFORE creating anything, so one bad
            // entry fails the whole request and nothing is half-saved.
            var guardianInputs = command.Guardians ?? new List<StudentGuardianInput>();
            var resolvedGuardians = new List<Guardian>();
            var seenGuardianIds = new List<Guid>();
            foreach (var guardianInput in guardianInputs)
            {
                var relationshipCode = guardianInput.RelationshipCode.Trim();
                var relationshipExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.GuardianRelationship, relationshipCode, cancellationToken);
                if (!relationshipExists)
                {
                    var invalidRelationshipResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.ValidationError, "RelationshipCode '" + relationshipCode + "' is not a known relationship option.");
                    return invalidRelationshipResponse;
                }

                if (guardianInput.GuardianId.HasValue)
                {
                    if (seenGuardianIds.Contains(guardianInput.GuardianId.Value))
                    {
                        var duplicateGuardianResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.ValidationError, "Guardian '" + guardianInput.GuardianId.Value + "' appears more than once.");
                        return duplicateGuardianResponse;
                    }

                    var existingGuardian = await _unitOfWork.Guardians.GetByIdAsync(guardianInput.GuardianId.Value, cancellationToken);
                    if (existingGuardian == null)
                    {
                        var guardianNotFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, "Guardian with id '" + guardianInput.GuardianId.Value + "' was not found.");
                        return guardianNotFoundResponse;
                    }

                    seenGuardianIds.Add(guardianInput.GuardianId.Value);
                    resolvedGuardians.Add(existingGuardian);
                }
                else
                {
                    var newGuardian = new Guardian
                    {
                        FirstName = guardianInput.FirstName.Trim(),
                        LastName = guardianInput.LastName.Trim(),
                        Email = guardianInput.Email?.Trim(),
                        Phone = guardianInput.Phone?.Trim(),
                        Occupation = guardianInput.Occupation?.Trim(),
                        Address = guardianInput.Address?.Trim()
                    };

                    await _unitOfWork.Guardians.AddAsync(newGuardian, cancellationToken);
                    resolvedGuardians.Add(newGuardian);
                }
            }

            var student = new Student
            {
                AdmissionNo = trimmedAdmissionNo,
                FirstName = command.FirstName.Trim(),
                MiddleName = command.MiddleName?.Trim(),
                LastName = command.LastName.Trim(),
                Gender = command.Gender,
                DateOfBirth = command.DateOfBirth,
                Email = command.Email?.Trim(),
                Phone = command.Phone?.Trim(),
                Address = command.Address?.Trim(),
                AdmissionDate = command.AdmissionDate,
                Status = RecordStatus.Active
            };

            await _unitOfWork.Students.AddAsync(student, cancellationToken);

            // Student + new guardians + links all land in one SaveChanges, so onboarding is
            // all-or-nothing.
            var guardianLinks = new List<StudentGuardian>();
            for (var index = 0; index < guardianInputs.Count; index++)
            {
                var link = new StudentGuardian
                {
                    Student = student,
                    Guardian = resolvedGuardians[index],
                    RelationshipCode = guardianInputs[index].RelationshipCode.Trim(),
                    IsPrimary = guardianInputs[index].IsPrimary
                };

                await _unitOfWork.Students.AddGuardianLinkAsync(link, cancellationToken);
                guardianLinks.Add(link);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var studentDto = StudentMapper.ToDto(student, guardianLinks);
            var successResponse = CommonResponse<StudentDto>.Success(studentDto, "Student created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<StudentDto>> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, "Student with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            // The profile includes guardian details -- the detail screen shows them inline.
            var guardianLinks = await _unitOfWork.Students.GetGuardianLinksAsync(id, cancellationToken);

            var studentDto = StudentMapper.ToDto(student, guardianLinks);
            studentDto.CurrentEnrollment = await BuildCurrentEnrollmentAsync(id, cancellationToken);

            var successResponse = CommonResponse<StudentDto>.Success(studentDto);
            return successResponse;
        }

        // The profile's "current class" block: the active enrollment (preferring the IsCurrent
        // academic year, falling back to the latest year by start date) plus the subjects being
        // studied -- every mandatory subject the section sees, and the chosen electives.
        private async Task<StudentCurrentEnrollmentDto> BuildCurrentEnrollmentAsync(Guid studentId, CancellationToken cancellationToken)
        {
            var activeEnrollments = await _unitOfWork.Enrollments.GetActiveByStudentAsync(studentId, cancellationToken);
            if (activeEnrollments.Count == 0)
            {
                return null;
            }

            Enrollment currentEnrollment = null;
            foreach (var enrollment in activeEnrollments)
            {
                if (currentEnrollment == null)
                {
                    currentEnrollment = enrollment;
                    continue;
                }

                var candidateYear = enrollment.ClassSection.AcademicClass.AcademicYear;
                var selectedYear = currentEnrollment.ClassSection.AcademicClass.AcademicYear;
                if (candidateYear.IsCurrent && !selectedYear.IsCurrent)
                {
                    currentEnrollment = enrollment;
                    continue;
                }

                if (candidateYear.IsCurrent == selectedYear.IsCurrent && candidateYear.StartDate > selectedYear.StartDate)
                {
                    currentEnrollment = enrollment;
                }
            }

            var classSection = currentEnrollment.ClassSection;
            var academicClass = classSection.AcademicClass;
            var academicYear = academicClass.AcademicYear;

            // Effective subject list for the section, then keep mandatory rows plus the
            // electives this enrollment actually picked.
            var sectionSubjects = await _unitOfWork.AcademicClasses.GetClassSubjectsAsync(academicClass.Id, classSection.Id, cancellationToken);
            var electiveSubjects = await _unitOfWork.Enrollments.GetElectiveSubjectsAsync(currentEnrollment.Id, cancellationToken);

            var electedClassSubjectIds = new List<Guid>();
            foreach (var electiveSubject in electiveSubjects)
            {
                electedClassSubjectIds.Add(electiveSubject.ClassSubjectId);
            }

            var subjectDtos = new List<StudentSubjectDto>();
            foreach (var classSubject in sectionSubjects)
            {
                if (!classSubject.IsMandatory && !electedClassSubjectIds.Contains(classSubject.Id))
                {
                    continue;
                }

                var subjectDto = new StudentSubjectDto
                {
                    ClassSubjectId = classSubject.Id,
                    SubjectCode = classSubject.SubjectCode,
                    IsMandatory = classSubject.IsMandatory,
                    DisplayOrder = classSubject.DisplayOrder
                };
                subjectDtos.Add(subjectDto);
            }

            var currentEnrollmentDto = new StudentCurrentEnrollmentDto
            {
                EnrollmentId = currentEnrollment.Id,
                AcademicYearId = academicYear.Id,
                AcademicYearCode = academicYear.Code,
                AcademicYearName = academicYear.Name,
                AcademicClassId = academicClass.Id,
                GradeCode = academicClass.GradeCode,
                ClassSectionId = classSection.Id,
                SectionCode = classSection.SectionCode,
                RollNumber = currentEnrollment.RollNumber,
                EnrollmentDate = currentEnrollment.EnrollmentDate,
                Subjects = subjectDtos
            };

            return currentEnrollmentDto;
        }

        public async Task<CommonResponse<PaginatedResponse<StudentDto>>> GetStudentsAsync(GetStudentsQuery query, CancellationToken cancellationToken = default)
        {
            var pagedStudents = await _unitOfWork.Students.GetPagedByFilterAsync(query.Search, query.Page, query.PageSize, cancellationToken);

            var studentDtos = new List<StudentDto>();
            foreach (var student in pagedStudents.Items)
            {
                var studentDto = StudentMapper.ToDto(student);
                studentDtos.Add(studentDto);
            }

            var paginatedResponse = new PaginatedResponse<StudentDto>
            {
                Items = studentDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedStudents.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<StudentDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<StudentDto>> UpdateStudentAsync(Guid id, UpdateStudentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, "Student with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            student.FirstName = command.FirstName.Trim();
            student.MiddleName = command.MiddleName?.Trim();
            student.LastName = command.LastName.Trim();
            student.Gender = command.Gender;
            student.DateOfBirth = command.DateOfBirth;
            student.Email = command.Email?.Trim();
            student.Phone = command.Phone?.Trim();
            student.Address = command.Address?.Trim();
            student.AdmissionDate = command.AdmissionDate;
            student.Status = command.Status;

            // Guardians is three-way: null = untouched, [] = unlink all, list = replace-sync.
            if (command.Guardians != null)
            {
                var syncFailure = await SyncGuardianLinksAsync(student, command.Guardians, cancellationToken);
                if (syncFailure != null)
                {
                    return syncFailure;
                }
            }

            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var guardianLinks = await _unitOfWork.Students.GetGuardianLinksAsync(id, cancellationToken);

            var studentDto = StudentMapper.ToDto(student, guardianLinks);
            var successResponse = CommonResponse<StudentDto>.Success(studentDto, "Student updated successfully.");
            return successResponse;
        }

        // Replace-sync of a student's guardian links against the submitted list. Everything is
        // validated/resolved before the first mutation, so a bad entry fails the whole update.
        // Returns null on success, or the failure response to bubble up.
        private async Task<CommonResponse<StudentDto>> SyncGuardianLinksAsync(Student student, List<StudentGuardianInput> guardianInputs, CancellationToken cancellationToken)
        {
            var existingLinks = await _unitOfWork.Students.GetGuardianLinksAsync(student.Id, cancellationToken);

            var referencedGuardianIds = new List<Guid>();
            var resolvedGuardians = new Guardian[guardianInputs.Count];
            for (var index = 0; index < guardianInputs.Count; index++)
            {
                var guardianInput = guardianInputs[index];

                var relationshipCode = guardianInput.RelationshipCode.Trim();
                var relationshipExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.GuardianRelationship, relationshipCode, cancellationToken);
                if (!relationshipExists)
                {
                    var invalidRelationshipResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.ValidationError, "RelationshipCode '" + relationshipCode + "' is not a known relationship option.");
                    return invalidRelationshipResponse;
                }

                if (guardianInput.GuardianId.HasValue)
                {
                    if (referencedGuardianIds.Contains(guardianInput.GuardianId.Value))
                    {
                        var duplicateResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.ValidationError, "Guardian '" + guardianInput.GuardianId.Value + "' appears more than once.");
                        return duplicateResponse;
                    }

                    referencedGuardianIds.Add(guardianInput.GuardianId.Value);

                    var alreadyLinked = false;
                    foreach (var link in existingLinks)
                    {
                        if (link.GuardianId == guardianInput.GuardianId.Value)
                        {
                            alreadyLinked = true;
                        }
                    }

                    if (!alreadyLinked)
                    {
                        var guardian = await _unitOfWork.Guardians.GetByIdAsync(guardianInput.GuardianId.Value, cancellationToken);
                        if (guardian == null)
                        {
                            var guardianNotFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, "Guardian with id '" + guardianInput.GuardianId.Value + "' was not found.");
                            return guardianNotFoundResponse;
                        }

                        resolvedGuardians[index] = guardian;
                    }
                }
                else
                {
                    var newGuardian = new Guardian
                    {
                        FirstName = guardianInput.FirstName.Trim(),
                        LastName = guardianInput.LastName.Trim(),
                        Email = guardianInput.Email?.Trim(),
                        Phone = guardianInput.Phone?.Trim(),
                        Occupation = guardianInput.Occupation?.Trim(),
                        Address = guardianInput.Address?.Trim()
                    };

                    resolvedGuardians[index] = newGuardian;
                }
            }

            // Mutation phase. Links absent from the submitted list go first, then the list is
            // applied: existing links get relationship/primary updated in place, the rest are
            // new links (to an existing guardian or a freshly created one).
            foreach (var existingLink in existingLinks)
            {
                if (!referencedGuardianIds.Contains(existingLink.GuardianId))
                {
                    _unitOfWork.Students.RemoveGuardianLink(existingLink);
                }
            }

            for (var index = 0; index < guardianInputs.Count; index++)
            {
                var guardianInput = guardianInputs[index];
                var relationshipCode = guardianInput.RelationshipCode.Trim();

                if (guardianInput.GuardianId.HasValue)
                {
                    StudentGuardian existingLink = null;
                    foreach (var link in existingLinks)
                    {
                        if (link.GuardianId == guardianInput.GuardianId.Value)
                        {
                            existingLink = link;
                        }
                    }

                    if (existingLink != null)
                    {
                        existingLink.RelationshipCode = relationshipCode;
                        existingLink.IsPrimary = guardianInput.IsPrimary;
                        continue;
                    }
                }
                else
                {
                    await _unitOfWork.Guardians.AddAsync(resolvedGuardians[index], cancellationToken);
                }

                var newLink = new StudentGuardian
                {
                    Student = student,
                    Guardian = resolvedGuardians[index],
                    RelationshipCode = relationshipCode,
                    IsPrimary = guardianInput.IsPrimary
                };

                await _unitOfWork.Students.AddGuardianLinkAsync(newLink, cancellationToken);
            }

            return null;
        }

        public async Task<CommonResponse<bool>> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Student with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            // Soft delete: enrollment history rows survive (they reference the student id), which
            // is the point of keeping student records soft-deleted.
            _unitOfWork.Students.Remove(student);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Student deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<StudentGuardianDto>> LinkGuardianAsync(Guid studentId, LinkGuardianCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _linkGuardianValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentGuardianDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                var studentNotFoundResponse = CommonResponse<StudentGuardianDto>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId + "' was not found.");
                return studentNotFoundResponse;
            }

            var guardian = await _unitOfWork.Guardians.GetByIdAsync(command.GuardianId, cancellationToken);
            if (guardian == null)
            {
                var guardianNotFoundResponse = CommonResponse<StudentGuardianDto>.Fail(ResponseCodes.NotFound, "Guardian with id '" + command.GuardianId + "' was not found.");
                return guardianNotFoundResponse;
            }

            var relationshipCode = command.RelationshipCode.Trim();
            var relationshipExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.GuardianRelationship, relationshipCode, cancellationToken);
            if (!relationshipExists)
            {
                var invalidRelationshipResponse = CommonResponse<StudentGuardianDto>.Fail(ResponseCodes.ValidationError, "RelationshipCode '" + relationshipCode + "' is not a known relationship option.");
                return invalidRelationshipResponse;
            }

            var linkExists = await _unitOfWork.Students.GuardianLinkExistsAsync(studentId, command.GuardianId, cancellationToken);
            if (linkExists)
            {
                var conflictResponse = CommonResponse<StudentGuardianDto>.Fail(ResponseCodes.Conflict, "This guardian is already linked to the student.");
                return conflictResponse;
            }

            // Single-primary invariant: promoting this link demotes any existing primary.
            if (command.IsPrimary)
            {
                var primaryLinks = await _unitOfWork.Students.GetPrimaryGuardianLinksAsync(studentId, cancellationToken);
                foreach (var primaryLink in primaryLinks)
                {
                    primaryLink.IsPrimary = false;
                }
            }

            var link = new StudentGuardian
            {
                StudentId = studentId,
                GuardianId = command.GuardianId,
                RelationshipCode = relationshipCode,
                IsPrimary = command.IsPrimary,
                Guardian = guardian
            };

            await _unitOfWork.Students.AddGuardianLinkAsync(link, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var linkDto = StudentMapper.ToGuardianLinkDto(link);
            var successResponse = CommonResponse<StudentGuardianDto>.Success(linkDto, "Guardian linked successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> UnlinkGuardianAsync(Guid studentId, Guid linkId, CancellationToken cancellationToken = default)
        {
            var link = await _unitOfWork.Students.GetGuardianLinkByIdAsync(linkId, cancellationToken);
            if (link == null || link.StudentId != studentId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Guardian link was not found on this student.");
                return notFoundResponse;
            }

            _unitOfWork.Students.RemoveGuardianLink(link);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Guardian unlinked successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<StudentGuardianDto>>> GetGuardiansAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<List<StudentGuardianDto>>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId + "' was not found.");
                return notFoundResponse;
            }

            var guardianLinks = await _unitOfWork.Students.GetGuardianLinksAsync(studentId, cancellationToken);

            var linkDtos = new List<StudentGuardianDto>();
            foreach (var guardianLink in guardianLinks)
            {
                var linkDto = StudentMapper.ToGuardianLinkDto(guardianLink);
                linkDtos.Add(linkDto);
            }

            var successResponse = CommonResponse<List<StudentGuardianDto>>.Success(linkDtos);
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
