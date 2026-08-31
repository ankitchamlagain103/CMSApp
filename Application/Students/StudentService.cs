using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Validation;
using Application.DocumentTemplates;
using Application.Employees.Dtos;
using Application.Exams;
using Application.Students.Commands;
using Application.Students.Dtos;
using Application.Students.Queries;
using Application.Students.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Students
{
    public class StudentService : IStudentService
    {
        // Same window/cap convention as EmployeeService.GetEmployeeDashboardAsync's own
        // dashboard constants -- kept separate (not shared) since the two dashboards are
        // unrelated features that happen to use the same numbers.
        private const int DashboardEventWindowDays = 30;
        private const int DashboardEventMaxCount = 10;
        private const int DashboardResultMaxCount = 5;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly IIdentityService _identityService;
        private readonly ICurrentUserService _currentUserService;
        private readonly CreateStudentCommandValidator _createValidator;
        private readonly UpdateStudentCommandValidator _updateValidator;
        private readonly LinkGuardianCommandValidator _linkGuardianValidator;
        private readonly UploadStudentDocumentCommandValidator _uploadDocumentValidator;

        public StudentService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            IIdentityService identityService,
            ICurrentUserService currentUserService,
            CreateStudentCommandValidator createValidator,
            UpdateStudentCommandValidator updateValidator,
            LinkGuardianCommandValidator linkGuardianValidator,
            UploadStudentDocumentCommandValidator uploadDocumentValidator)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _identityService = identityService;
            _currentUserService = currentUserService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _linkGuardianValidator = linkGuardianValidator;
            _uploadDocumentValidator = uploadDocumentValidator;
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

            // Blank admission number = backend-generated (ADM{year}{seq}); a supplied one (e.g.
            // migrated from an old system) is honored after the usual uniqueness check.
            //var trimmedAdmissionNo = command.AdmissionNo?.Trim();
            var trimmedAdmissionNo = string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedAdmissionNo))
            {
                var admissionNoPrefix = "ADM" + DateTime.UtcNow.Year;
                var existingAdmissionNos = await _unitOfWork.Students.GetAdmissionNosByPrefixAsync(admissionNoPrefix, cancellationToken);
                trimmedAdmissionNo = NumberSequenceHelper.Next(admissionNoPrefix, existingAdmissionNos, 3);
            }
            else
            {
                var admissionNoExists = await _unitOfWork.Students.AdmissionNoExistsAsync(trimmedAdmissionNo, cancellationToken);
                if (admissionNoExists)
                {
                    var conflictResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.Conflict, "Admission number '" + trimmedAdmissionNo + "' is already in use (possibly by a soft-deleted student).");
                    return conflictResponse;
                }
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

            var successMessage = "Student created successfully.";
            if (command.RegisterUserAccount)
            {
                var provisionResult = await ProvisionAccountOrErrorAsync(student.Email, student.Phone, student.FirstName, student.LastName, student.Gender, cancellationToken);
                if (provisionResult.ErrorResponseCode != null)
                {
                    var provisionFailureResponse = CommonResponse<StudentDto>.Fail(provisionResult.ErrorResponseCode, provisionResult.ErrorMessage);
                    return provisionFailureResponse;
                }

                student.UserId = provisionResult.UserId;
                successMessage = "Student created successfully. A portal account was created -- an activation email has been sent to " + student.Email + ".";
            }

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

            var relationshipLabels = await LoadRelationshipLabelMapAsync(cancellationToken);
            var studentDto = StudentMapper.ToDto(student, guardianLinks, relationshipLabels);
            var successResponse = CommonResponse<StudentDto>.Success(studentDto, successMessage);
            return successResponse;
        }

        // Portal account provisioning retrofit (2026-07-27) -- for a student that didn't get a
        // login at creation time.
        public async Task<CommonResponse<StudentDto>> RegisterUserAccountAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId + "' was not found.");
                return notFoundResponse;
            }

            if (student.UserId.HasValue)
            {
                var conflictResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.Conflict, "This student already has a portal account.");
                return conflictResponse;
            }

            if (string.IsNullOrWhiteSpace(student.Email))
            {
                var noEmailResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.ValidationError, "This student has no email on record.");
                return noEmailResponse;
            }

            var provisionResult = await ProvisionAccountOrErrorAsync(student.Email, student.Phone, student.FirstName, student.LastName, student.Gender, cancellationToken);
            if (provisionResult.ErrorResponseCode != null)
            {
                var provisionFailureResponse = CommonResponse<StudentDto>.Fail(provisionResult.ErrorResponseCode, provisionResult.ErrorMessage);
                return provisionFailureResponse;
            }

            student.UserId = provisionResult.UserId;
            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var studentDto = StudentMapper.ToDto(student);
            var successResponse = CommonResponse<StudentDto>.Success(studentDto, "Portal account created -- an activation email has been sent to " + student.Email + ".");
            return successResponse;
        }

        // Shared by CreateStudentAsync and RegisterUserAccountAsync -- checks the email isn't
        // already registered to another ApplicationUser (Conflict), then delegates to
        // IIdentityService.ProvisionPortalAccountAsync (ValidationError on any other failure).
        // Always the fixed RoleNames.Student role -- no admin role picker, unlike Employees.
        private async Task<(Guid? UserId, string ErrorResponseCode, string ErrorMessage)> ProvisionAccountOrErrorAsync(string email, string phone, string firstName, string lastName, Gender gender, CancellationToken cancellationToken)
        {
            var emailExists = await _identityService.EmailExistsAsync(email, cancellationToken);
            if (emailExists)
            {
                return (null, ResponseCodes.Conflict, "Email '" + email + "' is already registered to a portal account.");
            }

            var provisionRequest = new ProvisionPortalAccountRequest
            {
                Email = email,
                PhoneNumber = phone,
                FirstName = firstName,
                LastName = lastName,
                Gender = gender,
                RoleNames = new List<string> { RoleNames.Student }
            };

            var provisionResult = await _identityService.ProvisionPortalAccountAsync(provisionRequest, cancellationToken);
            if (!provisionResult.Succeeded)
            {
                var combinedMessage = string.Join(" ", provisionResult.Errors);
                return (null, ResponseCodes.ValidationError, combinedMessage);
            }

            return (Guid.Parse(provisionResult.UserId), null, null);
        }

        // 2026-08-05: slimmed to a profile-header shape -- Guardians (now GET .../guardians),
        // full subject/teacher timetable (now GET .../timetable), and enrollment history (now
        // GET .../enrollment-history) all moved to their own tab-scoped endpoints, so opening a
        // student's profile no longer pays for data that most page loads never look at.
        public async Task<CommonResponse<StudentDto>> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, "Student with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var studentDto = StudentMapper.ToDto(student);
            studentDto.CurrentEnrollment = await BuildCurrentEnrollmentAsync(id, cancellationToken);

            var successResponse = CommonResponse<StudentDto>.Success(studentDto);
            return successResponse;
        }

        // "History" tab -- every enrollment ever, oldest year first. Split out of
        // GetStudentByIdAsync 2026-08-05 (see that method's own comment).
        public async Task<CommonResponse<List<StudentEnrollmentHistoryDto>>> GetEnrollmentHistoryAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<List<StudentEnrollmentHistoryDto>>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId + "' was not found.");
                return notFoundResponse;
            }

            var classLabels = await LoadClassLabelMapAsync(cancellationToken);
            var enrollmentHistory = await _unitOfWork.Enrollments.GetHistoryByStudentAsync(studentId, cancellationToken);

            var historyDtos = new List<StudentEnrollmentHistoryDto>();
            foreach (var enrollment in enrollmentHistory)
            {
                var historyDto = StudentMapper.ToEnrollmentHistoryDto(enrollment, classLabels);
                historyDtos.Add(historyDto);
            }

            var successResponse = CommonResponse<List<StudentEnrollmentHistoryDto>>.Success(historyDtos);
            return successResponse;
        }

        // "Current Class" tab -- the student's own subject/teacher/period routine. Split out of
        // the old BuildCurrentEnrollmentAsync (which used to embed this in every profile GET)
        // 2026-08-05: reuses the same repository method
        // (IEmployeeRepository.GetAssignmentsByAcademicClassAsync) the "who teaches this class"
        // admin endpoint (GET /api/academicclasses/{id}/teacher-assignments) already uses, scoped
        // down to this student's own section, then filtered to the subjects they actually study.
        public async Task<CommonResponse<StudentTimetableDto>> GetTimetableAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<StudentTimetableDto>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId + "' was not found.");
                return notFoundResponse;
            }

            var currentEnrollment = await ResolveActiveEnrollmentAsync(studentId, cancellationToken);
            if (currentEnrollment == null)
            {
                var noEnrollmentResponse = CommonResponse<StudentTimetableDto>.Fail(ResponseCodes.NotFound, "This student has no active enrollment to build a timetable from.");
                return noEnrollmentResponse;
            }

            var classSection = currentEnrollment.ClassSection;
            var academicClass = classSection.AcademicClass;
            var academicYear = academicClass.AcademicYear;

            var studyingSubjects = await ResolveStudyingSubjectsAsync(academicClass.Id, classSection.Id, currentEnrollment.Id, cancellationToken);

            // Every assignment for this exact section -- Employee, ClassSubject, ClassSection,
            // TimePeriod all pre-loaded, the same repository call the class-scoped "who teaches
            // this class" admin endpoint uses.
            var sectionAssignments = await _unitOfWork.Employees.GetAssignmentsByAcademicClassAsync(academicClass.Id, classSection.Id, cancellationToken);

            var classLabels = await LoadClassLabelMapAsync(cancellationToken);
            ConfigLabelHelper.MergeLabelMap(classLabels, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.Subject, cancellationToken));

            string classTeacherName = null;
            foreach (var assignment in sectionAssignments)
            {
                if (assignment.IsClassTeacher && assignment.Employee != null)
                {
                    classTeacherName = BuildTeacherFullName(assignment.Employee);
                    break;
                }
            }

            var entries = new List<StudentTimetableEntryDto>();
            foreach (var classSubject in studyingSubjects)
            {
                var entry = BuildTimetableEntry(classSubject, sectionAssignments, classLabels);
                entries.Add(entry);
            }

            // Routine order: whichever subjects have a period land first, ordered by start time;
            // subjects with no period assigned yet fall back to subject code.
            entries.Sort(CompareTimetableEntries);

            var timetableDto = new StudentTimetableDto
            {
                EnrollmentId = currentEnrollment.Id,
                AcademicYearId = academicYear.Id,
                AcademicYearCode = academicYear.Code,
                AcademicYearName = academicYear.Name,
                AcademicClassId = academicClass.Id,
                GradeCode = academicClass.GradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(classLabels, academicClass.GradeCode),
                ClassSectionId = classSection.Id,
                SectionCode = classSection.SectionCode,
                SectionLabel = ConfigLabelHelper.Resolve(classLabels, classSection.SectionCode),
                ClassTeacherName = classTeacherName,
                Entries = entries
            };

            var successResponse = CommonResponse<StudentTimetableDto>.Success(timetableDto);
            return successResponse;
        }

        private static int CompareTimetableEntries(StudentTimetableEntryDto first, StudentTimetableEntryDto second)
        {
            if (first.TimePeriodStartTime.HasValue && second.TimePeriodStartTime.HasValue)
            {
                return first.TimePeriodStartTime.Value.CompareTo(second.TimePeriodStartTime.Value);
            }

            if (first.TimePeriodStartTime.HasValue != second.TimePeriodStartTime.HasValue)
            {
                return first.TimePeriodStartTime.HasValue ? -1 : 1;
            }

            return string.Compare(first.SubjectCode, second.SubjectCode, StringComparison.Ordinal);
        }

        // Every assignment matching this ClassSubject in the given section -- several teachers
        // can legitimately co-teach the same subject/section (see StudentTimetableEntryDto's own
        // doc comment for how that's simplified into one row).
        private static StudentTimetableEntryDto BuildTimetableEntry(ClassSubject classSubject, IReadOnlyList<TeacherAssignment> sectionAssignments, IReadOnlyDictionary<string, string> classLabelsByCode)
        {
            var matchingAssignments = new List<TeacherAssignment>();
            foreach (var assignment in sectionAssignments)
            {
                if (assignment.ClassSubjectId == classSubject.Id)
                {
                    matchingAssignments.Add(assignment);
                }
            }

            var teacherNames = new List<string>();
            foreach (var assignment in matchingAssignments)
            {
                if (assignment.Employee != null)
                {
                    var fullName = BuildTeacherFullName(assignment.Employee);
                    if (!teacherNames.Contains(fullName))
                    {
                        teacherNames.Add(fullName);
                    }
                }
            }

            var primaryAssignment = matchingAssignments.Count > 0 ? matchingAssignments[0] : null;

            var entry = new StudentTimetableEntryDto
            {
                ClassSubjectId = classSubject.Id,
                SubjectCode = classSubject.SubjectCode,
                SubjectLabel = ConfigLabelHelper.Resolve(classLabelsByCode, classSubject.SubjectCode),
                IsMandatory = classSubject.IsMandatory,
                TeacherId = primaryAssignment != null ? primaryAssignment.TeacherId : (Guid?)null,
                TeacherName = teacherNames.Count > 0 ? string.Join(", ", teacherNames) : null,
                EmployeeCode = primaryAssignment != null && primaryAssignment.Employee != null ? primaryAssignment.Employee.EmployeeCode : null,
                TimePeriodId = primaryAssignment != null ? primaryAssignment.TimePeriodId : (Guid?)null,
                TimePeriodName = primaryAssignment != null && primaryAssignment.TimePeriod != null ? primaryAssignment.TimePeriod.Name : null,
                TimePeriodStartTime = primaryAssignment != null && primaryAssignment.TimePeriod != null ? primaryAssignment.TimePeriod.StartTime : (TimeSpan?)null,
                TimePeriodEndTime = primaryAssignment != null && primaryAssignment.TimePeriod != null ? primaryAssignment.TimePeriod.EndTime : (TimeSpan?)null
            };

            return entry;
        }

        public async Task<CommonResponse<DocumentPreviewDto>> GetIdCardPreviewAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var studentResponse = await GetStudentByIdAsync(studentId, cancellationToken);
            if (studentResponse.Data == null)
            {
                var studentFailureResponse = CommonResponse<DocumentPreviewDto>.Fail(studentResponse.ResponseCode, studentResponse.ResponseMessage);
                return studentFailureResponse;
            }

            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByTemplateTypeAsync(DocumentTemplateType.StudentIdCard, cancellationToken);
            if (documentTemplate == null)
            {
                var noTemplateResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "No document template is configured for '" + DocumentTemplateType.StudentIdCard + "' yet.");
                return noTemplateResponse;
            }

            var studentDto = studentResponse.Data;
            var studentName = studentDto.FirstName + " " + studentDto.LastName;

            // GetStudentByIdAsync's response no longer carries Guardians (moved to its own
            // dedicated GET .../guardians endpoint, 2026-08-05) -- this internal composition
            // fetches them directly instead of going through the trimmed public DTO.
            var guardianLinks = await _unitOfWork.Students.GetGuardianLinksAsync(studentId, cancellationToken);
            StudentGuardian primaryGuardianLink = null;
            foreach (var guardianLink in guardianLinks)
            {
                if (guardianLink.IsPrimary)
                {
                    primaryGuardianLink = guardianLink;
                    break;
                }
            }

            var guardianName = primaryGuardianLink != null && primaryGuardianLink.Guardian != null ? primaryGuardianLink.Guardian.FirstName + " " + primaryGuardianLink.Guardian.LastName : string.Empty;
            var guardianPhone = primaryGuardianLink != null && primaryGuardianLink.Guardian != null ? primaryGuardianLink.Guardian.Phone : string.Empty;

            var currentEnrollment = studentDto.CurrentEnrollment;

            var placeholderValues = new Dictionary<string, string>
            {
                { "StudentName", studentName },
                { "AdmissionNo", studentDto.AdmissionNo },
                { "GradeCode", currentEnrollment != null ? currentEnrollment.GradeCode : string.Empty },
                { "SectionCode", currentEnrollment != null ? currentEnrollment.SectionCode : string.Empty },
                { "RollNumber", currentEnrollment != null ? currentEnrollment.RollNumber : string.Empty },
                { "DateOfBirth", studentDto.DateOfBirth.HasValue ? studentDto.DateOfBirth.Value.ToString("yyyy-MM-dd") : string.Empty },
                { "GuardianName", guardianName },
                { "GuardianPhone", guardianPhone }
            };

            var renderedHtml = TemplateRenderer.Render(documentTemplate.HtmlContent, placeholderValues);

            var documentPreviewDto = new DocumentPreviewDto
            {
                TemplateType = DocumentTemplateType.StudentIdCard,
                Html = renderedHtml
            };

            var previewSuccessResponse = CommonResponse<DocumentPreviewDto>.Success(documentPreviewDto);
            return previewSuccessResponse;
        }

        // The profile header's lightweight "current class" indicator -- the active enrollment
        // (preferring the IsCurrent academic year, falling back to the latest year by start
        // date), no subject list (see StudentCurrentEnrollmentDto's own doc comment -- that
        // moved to GetTimetableAsync 2026-08-05).
        private async Task<StudentCurrentEnrollmentDto> BuildCurrentEnrollmentAsync(Guid studentId, CancellationToken cancellationToken)
        {
            var currentEnrollment = await ResolveActiveEnrollmentAsync(studentId, cancellationToken);
            if (currentEnrollment == null)
            {
                return null;
            }

            var classSection = currentEnrollment.ClassSection;
            var academicClass = classSection.AcademicClass;
            var academicYear = academicClass.AcademicYear;

            var classLabels = await LoadClassLabelMapAsync(cancellationToken);

            var currentEnrollmentDto = new StudentCurrentEnrollmentDto
            {
                EnrollmentId = currentEnrollment.Id,
                AcademicYearId = academicYear.Id,
                AcademicYearCode = academicYear.Code,
                AcademicYearName = academicYear.Name,
                AcademicClassId = academicClass.Id,
                GradeCode = academicClass.GradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(classLabels, academicClass.GradeCode),
                ClassSectionId = classSection.Id,
                SectionCode = classSection.SectionCode,
                SectionLabel = ConfigLabelHelper.Resolve(classLabels, classSection.SectionCode),
                RollNumber = currentEnrollment.RollNumber,
                EnrollmentDate = currentEnrollment.EnrollmentDate
            };

            return currentEnrollmentDto;
        }

        // Shared by BuildCurrentEnrollmentAsync and GetTimetableAsync -- picks the active
        // (Status == Enrolled) enrollment to treat as "current": prefers the IsCurrent academic
        // year, tiebreaking on the latest year by start date. Null when the student has no active
        // enrollment anywhere.
        private async Task<Enrollment> ResolveActiveEnrollmentAsync(Guid studentId, CancellationToken cancellationToken)
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

            return currentEnrollment;
        }

        // Shared by GetTimetableAsync (and formerly BuildCurrentEnrollmentAsync, before its
        // subject list moved out) -- the effective subject list for the section, filtered down to
        // mandatory rows plus the electives this specific enrollment picked.
        private async Task<List<ClassSubject>> ResolveStudyingSubjectsAsync(Guid academicClassId, Guid classSectionId, Guid enrollmentId, CancellationToken cancellationToken)
        {
            var sectionSubjects = await _unitOfWork.AcademicClasses.GetClassSubjectsAsync(academicClassId, classSectionId, cancellationToken);
            var electiveSubjects = await _unitOfWork.Enrollments.GetElectiveSubjectsAsync(enrollmentId, cancellationToken);

            var electedClassSubjectIds = new List<Guid>();
            foreach (var electiveSubject in electiveSubjects)
            {
                electedClassSubjectIds.Add(electiveSubject.ClassSubjectId);
            }

            var studyingSubjects = new List<ClassSubject>();
            foreach (var classSubject in sectionSubjects)
            {
                if (!classSubject.IsMandatory && !electedClassSubjectIds.Contains(classSubject.Id))
                {
                    continue;
                }

                studyingSubjects.Add(classSubject);
            }

            return studyingSubjects;
        }

        // Merges Grade + Section labels -- shared by every place that needs to resolve
        // GradeCode/SectionCode to a display label (2026-08-05, part of moving Config
        // code->label resolution server-side instead of leaving it to per-caller UI lookups).
        private async Task<Dictionary<string, string>> LoadClassLabelMapAsync(CancellationToken cancellationToken)
        {
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.Grade, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.Section, cancellationToken));
            return labelsByCode;
        }

        // Same reasoning as LoadClassLabelMapAsync, for the GuardianRelationship catalog.
        private async Task<Dictionary<string, string>> LoadRelationshipLabelMapAsync(CancellationToken cancellationToken)
        {
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.GuardianRelationship, cancellationToken));
            return labelsByCode;
        }

        // Same reasoning as LoadClassLabelMapAsync, for the StudentDocumentType catalog.
        private async Task<Dictionary<string, string>> LoadStudentDocumentLabelMapAsync(CancellationToken cancellationToken)
        {
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.StudentDocumentType, cancellationToken));
            return labelsByCode;
        }

        public async Task<CommonResponse<PaginatedResponse<StudentDto>>> GetStudentsAsync(GetStudentsQuery query, CancellationToken cancellationToken = default)
        {
            var filter = BuildStudentFilter(query);
            return await GetStudentsInternalAsync(filter, query.Page, query.PageSize, cancellationToken);
        }

        // Self-service (2026-08-07): "a teacher should see his or her students details only" --
        // scopes the same GetStudentsQuery filters (search/grade/gender/etc. still all apply) down
        // to only the ClassSectionIds the caller actually has a TeacherAssignment for. A
        // non-teaching employee, or one with no assignments yet, gets an empty page, not an error.
        public async Task<CommonResponse<PaginatedResponse<StudentDto>>> GetMyStudentsAsync(GetStudentsQuery query, CancellationToken cancellationToken = default)
        {
            var (employeeId, errorMessage) = await ResolveCurrentEmployeeIdAsync(cancellationToken);
            if (!employeeId.HasValue)
            {
                var notFoundResponse = CommonResponse<PaginatedResponse<StudentDto>>.Fail(ResponseCodes.NotFound, errorMessage);
                return notFoundResponse;
            }

            var sectionIds = await ResolveAssignedClassSectionIdsAsync(employeeId.Value, cancellationToken);
            if (sectionIds.Count == 0)
            {
                var emptyPage = new PaginatedResponse<StudentDto>
                {
                    Items = new List<StudentDto>(),
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = 0
                };
                var emptySuccessResponse = CommonResponse<PaginatedResponse<StudentDto>>.Success(emptyPage);
                return emptySuccessResponse;
            }

            var filter = BuildStudentFilter(query);
            filter.ClassSectionIds = sectionIds;
            return await GetStudentsInternalAsync(filter, query.Page, query.PageSize, cancellationToken);
        }

        // Self-service (2026-08-07) single-student detail, scoped the same way as the list above.
        public async Task<CommonResponse<StudentDto>> GetMyStudentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var (employeeId, errorMessage) = await ResolveCurrentEmployeeIdAsync(cancellationToken);
            if (!employeeId.HasValue)
            {
                var notFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, errorMessage);
                return notFoundResponse;
            }

            var sectionIds = await ResolveAssignedClassSectionIdsAsync(employeeId.Value, cancellationToken);

            var studentResponse = await GetStudentByIdAsync(id, cancellationToken);
            if (studentResponse.ResponseCode != ResponseCodes.Success)
            {
                return studentResponse;
            }

            var studentSectionId = studentResponse.Data.CurrentEnrollment?.ClassSectionId;
            if (!studentSectionId.HasValue || !sectionIds.Contains(studentSectionId.Value))
            {
                var forbiddenResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.Forbidden, "This student is not in one of your assigned sections.");
                return forbiddenResponse;
            }

            return studentResponse;
        }

        // Student Portal self-service (resolve-then-delegate, same shape as
        // IEmployeeService's own "Me" methods): each one resolves the caller's own studentId
        // from the JWT, then calls the existing id-taking method unchanged -- response shapes
        // stay byte-for-byte identical to the admin {id}-scoped routes.

        private async Task<(Guid? StudentId, string ErrorMessage)> ResolveCurrentStudentIdAsync(CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return (null, "No authenticated user.");
            }

            var student = await _unitOfWork.Students.GetByUserIdAsync(userId.Value, cancellationToken);
            if (student == null)
            {
                return (null, "Your account is not linked to a student record.");
            }

            return (student.Id, null);
        }

        public async Task<CommonResponse<StudentDto>> GetMyProfileAsync(CancellationToken cancellationToken = default)
        {
            var (studentId, errorMessage) = await ResolveCurrentStudentIdAsync(cancellationToken);
            if (!studentId.HasValue)
            {
                var notFoundResponse = CommonResponse<StudentDto>.Fail(ResponseCodes.NotFound, errorMessage);
                return notFoundResponse;
            }

            return await GetStudentByIdAsync(studentId.Value, cancellationToken);
        }

        public async Task<CommonResponse<StudentTimetableDto>> GetMyTimetableAsync(CancellationToken cancellationToken = default)
        {
            var (studentId, errorMessage) = await ResolveCurrentStudentIdAsync(cancellationToken);
            if (!studentId.HasValue)
            {
                var notFoundResponse = CommonResponse<StudentTimetableDto>.Fail(ResponseCodes.NotFound, errorMessage);
                return notFoundResponse;
            }

            return await GetTimetableAsync(studentId.Value, cancellationToken);
        }

        public async Task<CommonResponse<List<StudentEnrollmentHistoryDto>>> GetMyEnrollmentHistoryAsync(CancellationToken cancellationToken = default)
        {
            var (studentId, errorMessage) = await ResolveCurrentStudentIdAsync(cancellationToken);
            if (!studentId.HasValue)
            {
                var notFoundResponse = CommonResponse<List<StudentEnrollmentHistoryDto>>.Fail(ResponseCodes.NotFound, errorMessage);
                return notFoundResponse;
            }

            return await GetEnrollmentHistoryAsync(studentId.Value, cancellationToken);
        }

        public async Task<CommonResponse<StudentDashboardDto>> GetMyDashboardAsync(CancellationToken cancellationToken = default)
        {
            var (studentId, errorMessage) = await ResolveCurrentStudentIdAsync(cancellationToken);
            if (!studentId.HasValue)
            {
                var notFoundResponse = CommonResponse<StudentDashboardDto>.Fail(ResponseCodes.NotFound, errorMessage);
                return notFoundResponse;
            }

            var student = await _unitOfWork.Students.GetByIdAsync(studentId.Value, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<StudentDashboardDto>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId.Value + "' was not found.");
                return notFoundResponse;
            }

            var dashboardDto = new StudentDashboardDto
            {
                StudentId = student.Id,
                StudentName = BuildStudentFullName(student.FirstName, student.MiddleName, student.LastName)
            };

            dashboardDto.CurrentEnrollment = await BuildCurrentEnrollmentAsync(studentId.Value, cancellationToken);

            var activeEnrollment = await ResolveActiveEnrollmentAsync(studentId.Value, cancellationToken);
            if (activeEnrollment != null)
            {
                var openInvoices = await _unitOfWork.FeeInvoices.GetOpenByEnrollmentAsync(activeEnrollment.Id, cancellationToken);
                DateTime? nextDueDate = null;
                foreach (var invoice in openInvoices)
                {
                    var balance = invoice.NetAmount - invoice.PaidAmount;
                    dashboardDto.TotalOutstandingAmount += balance;
                    if (!nextDueDate.HasValue || invoice.DueDate < nextDueDate.Value)
                    {
                        nextDueDate = invoice.DueDate;
                    }
                }

                dashboardDto.OpenInvoiceCount = openInvoices.Count;
                dashboardDto.NextFeeDueDate = nextDueDate;

                var recentResults = await _unitOfWork.ExamTerms.GetResultsPagedByFilterAsync(null, null, activeEnrollment.Id, 1, DashboardResultMaxCount, cancellationToken);
                foreach (var result in recentResults.Items)
                {
                    dashboardDto.RecentResults.Add(ExamMapper.ToStudentResultDto(result));
                }
            }

            var today = NepalDateHelper.GetNepalToday();
            var windowEnd = today.AddDays(DashboardEventWindowDays - 1);
            var upcomingEvents = new List<UpcomingEventDto>();

            if (student.DateOfBirth.HasValue)
            {
                var nextBirthday = RecurringDateHelper.ResolveNextOccurrence(student.DateOfBirth.Value, today);
                if (nextBirthday <= windowEnd)
                {
                    upcomingEvents.Add(new UpcomingEventDto { Type = "Birthday", Label = "Birthday", Date = nextBirthday });
                }
            }

            var calendarEvents = await _unitOfWork.CalendarEvents.GetActiveByAdDateRangeAsync(today, windowEnd, cancellationToken);
            foreach (var calendarEvent in calendarEvents)
            {
                if (calendarEvent.EventType != CalendarEventType.PublicHoliday && calendarEvent.EventType != CalendarEventType.InternalEvent)
                {
                    continue;
                }

                if (calendarEvent.ProvinceCode != null || calendarEvent.BranchCode != null)
                {
                    continue;
                }

                var eventType = calendarEvent.EventType == CalendarEventType.PublicHoliday ? "Holiday" : "Event";
                upcomingEvents.Add(new UpcomingEventDto { Type = eventType, Label = calendarEvent.Title, Date = calendarEvent.AdDate });
            }

            var festivals = await _unitOfWork.CalendarEvents.GetActiveFestivalsByAdDateRangeAsync(today, windowEnd, cancellationToken);
            foreach (var festival in festivals)
            {
                upcomingEvents.Add(new UpcomingEventDto { Type = "Festival", Label = festival.FestivalName, Date = festival.AdStartDate });
            }

            upcomingEvents.Sort((first, second) => DateTime.Compare(first.Date, second.Date));
            if (upcomingEvents.Count > DashboardEventMaxCount)
            {
                upcomingEvents.RemoveRange(DashboardEventMaxCount, upcomingEvents.Count - DashboardEventMaxCount);
            }

            dashboardDto.UpcomingEvents = upcomingEvents;

            var successResponse = CommonResponse<StudentDashboardDto>.Success(dashboardDto);
            return successResponse;
        }

        private static string BuildStudentFullName(string firstName, string middleName, string lastName)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                parts.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                parts.Add(middleName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                parts.Add(lastName);
            }

            return string.Join(" ", parts);
        }

        private static StudentFilter BuildStudentFilter(GetStudentsQuery query)
        {
            var filter = new StudentFilter
            {
                Search = query.Search,
                Phone = query.Phone,
                GradeCode = query.GradeCode,
                AcademicYearId = query.AcademicYearId,
                ClassSectionId = query.ClassSectionId,
                Status = query.Status,
                Gender = query.Gender,
                DateField = query.DateField,
                FromDate = query.FromDate,
                ToDate = query.ToDate
            };

            return filter;
        }

        private async Task<CommonResponse<PaginatedResponse<StudentDto>>> GetStudentsInternalAsync(StudentFilter filter, int page, int pageSize, CancellationToken cancellationToken)
        {
            var pagedStudents = await _unitOfWork.Students.GetPagedByFilterAsync(filter, page, pageSize, cancellationToken);

            var studentDtos = new List<StudentDto>();
            foreach (var student in pagedStudents.Items)
            {
                var studentDto = StudentMapper.ToDto(student);
                studentDtos.Add(studentDto);
            }

            var paginatedResponse = new PaginatedResponse<StudentDto>
            {
                Items = studentDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedStudents.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<StudentDto>>.Success(paginatedResponse);
            return successResponse;
        }

        private async Task<(Guid? EmployeeId, string ErrorMessage)> ResolveCurrentEmployeeIdAsync(CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return (null, "No authenticated user.");
            }

            var employee = await _unitOfWork.Employees.GetByUserIdAsync(userId.Value, cancellationToken);
            if (employee == null)
            {
                return (null, "Your account is not linked to an employee record.");
            }

            return (employee.Id, null);
        }

        // Every distinct ClassSectionId the given employee has a TeacherAssignment for. A legacy
        // class-wide assignment (ClassSectionId null, not creatable since 2026-08-04, see
        // TeacherAssignmentBuilder.BuildAsync) contributes nothing here -- there's no single
        // section to scope a "my students" list to, so it's skipped rather than resolved further.
        private async Task<List<Guid>> ResolveAssignedClassSectionIdsAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            var assignments = await _unitOfWork.Employees.GetAssignmentsAsync(employeeId, cancellationToken);
            var sectionIds = new List<Guid>();
            var seenSectionIds = new HashSet<Guid>();
            foreach (var assignment in assignments)
            {
                if (assignment.ClassSectionId.HasValue && seenSectionIds.Add(assignment.ClassSectionId.Value))
                {
                    sectionIds.Add(assignment.ClassSectionId.Value);
                }
            }

            return sectionIds;
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
            var relationshipLabels = await LoadRelationshipLabelMapAsync(cancellationToken);

            var studentDto = StudentMapper.ToDto(student, guardianLinks, relationshipLabels);
            var successResponse = CommonResponse<StudentDto>.Success(studentDto, "Student updated successfully.");
            return successResponse;
        }

        private static string BuildTeacherFullName(Employee employee)
        {
            var nameParts = new List<string>();
            nameParts.Add(employee.FirstName);
            if (!string.IsNullOrWhiteSpace(employee.MiddleName))
            {
                nameParts.Add(employee.MiddleName);
            }
            nameParts.Add(employee.LastName);

            var fullName = string.Join(" ", nameParts);
            return fullName;
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

            var relationshipLabels = await LoadRelationshipLabelMapAsync(cancellationToken);
            var linkDto = StudentMapper.ToGuardianLinkDto(link, relationshipLabels);
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
            var relationshipLabels = await LoadRelationshipLabelMapAsync(cancellationToken);

            var linkDtos = new List<StudentGuardianDto>();
            foreach (var guardianLink in guardianLinks)
            {
                var linkDto = StudentMapper.ToGuardianLinkDto(guardianLink, relationshipLabels);
                linkDtos.Add(linkDto);
            }

            var successResponse = CommonResponse<List<StudentGuardianDto>>.Success(linkDtos);
            return successResponse;
        }

        public async Task<CommonResponse<StudentDocumentDto>> UploadDocumentAsync(Guid studentId, UploadStudentDocumentCommand command, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default)
        {
            var validationResult = _uploadDocumentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentDocumentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            if (fileContent == null || fileSizeBytes <= 0)
            {
                var noFileResponse = CommonResponse<StudentDocumentDto>.Fail(ResponseCodes.ValidationError, "A document file is required.");
                return noFileResponse;
            }

            if (!DocumentFileRules.IsAllowedExtension(originalFileName))
            {
                var extensionResponse = CommonResponse<StudentDocumentDto>.Fail(ResponseCodes.ValidationError, "Unsupported file type. Allowed: " + DocumentFileRules.AllowedExtensionsDisplay() + ".");
                return extensionResponse;
            }

            if (fileSizeBytes > DocumentFileRules.MaxFileSizeBytes)
            {
                var sizeResponse = CommonResponse<StudentDocumentDto>.Fail(ResponseCodes.ValidationError, "File exceeds the maximum size of " + (DocumentFileRules.MaxFileSizeBytes / (1024 * 1024)) + " MB.");
                return sizeResponse;
            }

            var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<StudentDocumentDto>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId + "' was not found.");
                return notFoundResponse;
            }

            var documentTypeCode = command.DocumentTypeCode.Trim();
            var typeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.StudentDocumentType, documentTypeCode, cancellationToken);
            if (!typeExists)
            {
                var typeInvalidResponse = CommonResponse<StudentDocumentDto>.Fail(ResponseCodes.ValidationError, "DocumentTypeCode '" + documentTypeCode + "' is not a known student document type option.");
                return typeInvalidResponse;
            }

            var storedPath = await _fileStorage.SaveAsync(fileContent, originalFileName, "student-documents/" + studentId, cancellationToken);

            var document = new StudentDocument
            {
                StudentId = studentId,
                DocumentTypeCode = documentTypeCode,
                DocumentName = command.DocumentName.Trim(),
                FileName = originalFileName,
                FilePath = storedPath,
                ContentType = contentType,
                FileSizeBytes = fileSizeBytes,
                ValidUntil = command.ValidUntil,
                Remarks = command.Remarks?.Trim()
            };

            await _unitOfWork.Students.AddDocumentAsync(document, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // The row didn't land, so the stored file must not linger as an orphan.
                _fileStorage.Delete(storedPath);
                throw;
            }

            var documentLabels = await LoadStudentDocumentLabelMapAsync(cancellationToken);
            var documentDto = StudentMapper.ToDocumentDto(document, documentLabels);
            var successResponse = CommonResponse<StudentDocumentDto>.Success(documentDto, "Document uploaded successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<StudentDocumentDto>>> GetDocumentsAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                var notFoundResponse = CommonResponse<List<StudentDocumentDto>>.Fail(ResponseCodes.NotFound, "Student with id '" + studentId + "' was not found.");
                return notFoundResponse;
            }

            var documents = await _unitOfWork.Students.GetDocumentsAsync(studentId, cancellationToken);
            var documentLabels = await LoadStudentDocumentLabelMapAsync(cancellationToken);

            var documentDtos = new List<StudentDocumentDto>();
            foreach (var document in documents)
            {
                var documentDto = StudentMapper.ToDocumentDto(document, documentLabels);
                documentDtos.Add(documentDto);
            }

            var successResponse = CommonResponse<List<StudentDocumentDto>>.Success(documentDtos);
            return successResponse;
        }

        public async Task<CommonResponse<StudentDocumentFileDto>> GetDocumentFileAsync(Guid studentId, Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await _unitOfWork.Students.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null || document.StudentId != studentId)
            {
                var notFoundResponse = CommonResponse<StudentDocumentFileDto>.Fail(ResponseCodes.NotFound, "Document was not found on this student.");
                return notFoundResponse;
            }

            var contentStream = await _fileStorage.OpenReadAsync(document.FilePath, cancellationToken);
            if (contentStream == null)
            {
                var fileMissingResponse = CommonResponse<StudentDocumentFileDto>.Fail(ResponseCodes.NotFound, "The stored file for this document is missing.");
                return fileMissingResponse;
            }

            var fileDto = new StudentDocumentFileDto
            {
                Content = contentStream,
                ContentType = string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType,
                FileName = document.FileName
            };

            var successResponse = CommonResponse<StudentDocumentFileDto>.Success(fileDto);
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteDocumentAsync(Guid studentId, Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await _unitOfWork.Students.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null || document.StudentId != studentId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Document was not found on this student.");
                return notFoundResponse;
            }

            _unitOfWork.Students.RemoveDocument(document);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Only after the row is gone -- best-effort by contract.
            _fileStorage.Delete(document.FilePath);

            var successResponse = CommonResponse<bool>.Success(true, "Document deleted successfully.");
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
