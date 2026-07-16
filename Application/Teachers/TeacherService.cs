using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Validation;
using Application.DocumentTemplates;
using Application.Employees;
using Application.Employees.Commands;
using Application.Employees.Dtos;
using Application.Teachers.Commands;
using Application.Teachers.Dtos;
using Application.Teachers.Queries;
using Application.Teachers.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Teachers
{
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly IEmployeeService _employeeService;
        private readonly CreateTeacherCommandValidator _createValidator;
        private readonly UpdateTeacherCommandValidator _updateValidator;
        private readonly AddTeacherQualificationCommandValidator _addQualificationValidator;
        private readonly UploadTeacherDocumentCommandValidator _uploadDocumentValidator;

        public TeacherService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            IEmployeeService employeeService,
            CreateTeacherCommandValidator createValidator,
            UpdateTeacherCommandValidator updateValidator,
            AddTeacherQualificationCommandValidator addQualificationValidator,
            UploadTeacherDocumentCommandValidator uploadDocumentValidator)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _employeeService = employeeService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _addQualificationValidator = addQualificationValidator;
            _uploadDocumentValidator = uploadDocumentValidator;
        }

        public async Task<CommonResponse<TeacherDto>> CreateTeacherAsync(CreateTeacherCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TeacherDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var jobPositionCode = command.JobPositionCode.Trim();
            var positionExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.JobPosition, jobPositionCode, cancellationToken);
            if (!positionExists)
            {
                var invalidPositionResponse = CommonResponse<TeacherDto>.Fail(ResponseCodes.ValidationError, "JobPositionCode '" + jobPositionCode + "' is not a known job position option.");
                return invalidPositionResponse;
            }

            // Blank employee code = backend-generated (EMP{year}{seq}, shared sequence across
            // every employee type); a supplied one (e.g. migrated from an old system) is honored
            // after the usual uniqueness check.
            var trimmedEmployeeCode = command.EmployeeCode?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedEmployeeCode))
            {
                var employeeCodePrefix = "EMP" + DateTime.UtcNow.Year;
                var existingEmployeeCodes = await _unitOfWork.Employees.GetEmployeeCodesByPrefixAsync(employeeCodePrefix, cancellationToken);
                trimmedEmployeeCode = NumberSequenceHelper.Next(employeeCodePrefix, existingEmployeeCodes, 3);
            }
            else
            {
                var employeeCodeExists = await _unitOfWork.Employees.EmployeeCodeExistsAsync(trimmedEmployeeCode, cancellationToken);
                if (employeeCodeExists)
                {
                    var conflictResponse = CommonResponse<TeacherDto>.Fail(ResponseCodes.Conflict, "Employee code '" + trimmedEmployeeCode + "' is already in use (possibly by a soft-deleted employee).");
                    return conflictResponse;
                }
            }

            var employee = new Employee
            {
                EmployeeCode = trimmedEmployeeCode,
                FirstName = command.FirstName.Trim(),
                MiddleName = command.MiddleName?.Trim(),
                LastName = command.LastName.Trim(),
                Gender = command.Gender,
                DateOfBirth = command.DateOfBirth,
                Email = command.Email?.Trim(),
                Phone = command.Phone?.Trim(),
                JoinDate = command.JoinDate,
                EmployeeCategoryCode = EmployeeCategoryCodes.Academic,
                JobPositionCode = jobPositionCode,
                EmploymentStatus = EmploymentStatus.Active,
                BankName = command.BankName?.Trim(),
                BankAccountNumber = command.BankAccountNumber?.Trim(),
                PaymentMode = command.PaymentMode
            };

            var teacher = new Teacher
            {
                TeachingLicenseNo = command.TeachingLicenseNo?.Trim(),
                ExperienceYears = command.ExperienceYears,
                Specialization = command.Specialization?.Trim(),
                Employee = employee
            };
            employee.Teacher = teacher;

            await _unitOfWork.Employees.AddAsync(employee, cancellationToken);
            // Teacher.Id is assigned = Employee.Id via the shared-PK relationship fixup once
            // SaveChanges resolves the generated Employee.Id -- no explicit Teachers.AddAsync
            // needed, EF tracks it through the Employee.Teacher navigation.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var teacherDto = TeacherMapper.ToDto(teacher);
            var successResponse = CommonResponse<TeacherDto>.Success(teacherDto, "Teacher created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<TeacherDto>> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdWithEmployeeAsync(id, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<TeacherDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var teacherDto = TeacherMapper.ToDto(teacher);

            // Service history: the assignments with their academic years, oldest first -- the
            // first row (plus JoinDate) answers "teaching here since which year".
            var assignments = await _unitOfWork.Teachers.GetAssignmentsAsync(id, cancellationToken);
            foreach (var assignment in assignments)
            {
                var historyDto = TeacherMapper.ToServiceHistoryDto(assignment);
                teacherDto.ServiceHistory.Add(historyDto);
            }

            var successResponse = CommonResponse<TeacherDto>.Success(teacherDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<TeacherDto>>> GetTeachersAsync(GetTeachersQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new TeacherFilter
            {
                Search = query.Search,
                Phone = query.Phone,
                QualificationCode = query.QualificationCode,
                Status = query.Status,
                DateField = query.DateField,
                FromDate = query.FromDate,
                ToDate = query.ToDate
            };

            var pagedTeachers = await _unitOfWork.Teachers.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var teacherDtos = new List<TeacherDto>();
            foreach (var teacher in pagedTeachers.Items)
            {
                var teacherDto = TeacherMapper.ToDto(teacher);
                teacherDtos.Add(teacherDto);
            }

            var paginatedResponse = new PaginatedResponse<TeacherDto>
            {
                Items = teacherDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedTeachers.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<TeacherDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<TeacherDto>> UpdateTeacherAsync(Guid id, UpdateTeacherCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TeacherDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var teacher = await _unitOfWork.Teachers.GetByIdWithEmployeeAsync(id, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<TeacherDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var jobPositionCode = command.JobPositionCode.Trim();
            var positionExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.JobPosition, jobPositionCode, cancellationToken);
            if (!positionExists)
            {
                var invalidPositionResponse = CommonResponse<TeacherDto>.Fail(ResponseCodes.ValidationError, "JobPositionCode '" + jobPositionCode + "' is not a known job position option.");
                return invalidPositionResponse;
            }

            var employee = teacher.Employee;
            employee.FirstName = command.FirstName.Trim();
            employee.MiddleName = command.MiddleName?.Trim();
            employee.LastName = command.LastName.Trim();
            employee.Gender = command.Gender;
            employee.DateOfBirth = command.DateOfBirth;
            employee.Email = command.Email?.Trim();
            employee.Phone = command.Phone?.Trim();
            employee.JoinDate = command.JoinDate;
            employee.JobPositionCode = jobPositionCode;
            employee.EmploymentStatus = command.Status;
            employee.BankName = command.BankName?.Trim();
            employee.BankAccountNumber = command.BankAccountNumber?.Trim();
            employee.PaymentMode = command.PaymentMode;

            teacher.TeachingLicenseNo = command.TeachingLicenseNo?.Trim();
            teacher.ExperienceYears = command.ExperienceYears;
            teacher.Specialization = command.Specialization?.Trim();

            _unitOfWork.Employees.Update(employee);
            _unitOfWork.Teachers.Update(teacher);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var teacherDto = TeacherMapper.ToDto(teacher);
            var successResponse = CommonResponse<TeacherDto>.Success(teacherDto, "Teacher updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteTeacherAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(id, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Teacher with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var hasAssignments = await _unitOfWork.Teachers.HasAssignmentsAsync(id, cancellationToken);
            if (hasAssignments)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This teacher still has class assignments. Remove them first.");
                return conflictResponse;
            }

            // Teacher itself has no soft-delete lifecycle anymore -- deleting a teacher soft-
            // deletes the underlying Employee (same "records, not accounts" semantics as before:
            // the teacher becomes invisible/inactive), leaving the Teacher profile row and its
            // history (qualifications/documents) intact, consistent with how soft-deleting a
            // parent elsewhere in this codebase preserves child history.
            var employee = await _unitOfWork.Employees.GetByIdAsync(id, cancellationToken);
            _unitOfWork.Employees.Remove(employee);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Teacher deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<TeacherQualificationDto>> AddQualificationAsync(Guid teacherId, AddTeacherQualificationCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _addQualificationValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TeacherQualificationDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<TeacherQualificationDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var qualificationCode = command.QualificationCode.Trim();
            var qualificationCodeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.TeacherQualification, qualificationCode, cancellationToken);
            if (!qualificationCodeExists)
            {
                var invalidCodeResponse = CommonResponse<TeacherQualificationDto>.Fail(ResponseCodes.ValidationError, "QualificationCode '" + qualificationCode + "' is not a known qualification option.");
                return invalidCodeResponse;
            }

            var qualification = new TeacherQualification
            {
                TeacherId = teacherId,
                QualificationCode = qualificationCode,
                CourseName = command.CourseName?.Trim(),
                Institution = command.Institution?.Trim(),
                CompletionYear = command.CompletionYear,
                Score = command.Score?.Trim(),
                Remarks = command.Remarks?.Trim()
            };

            await _unitOfWork.Teachers.AddQualificationAsync(qualification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var qualificationDto = TeacherMapper.ToQualificationDto(qualification);
            var successResponse = CommonResponse<TeacherQualificationDto>.Success(qualificationDto, "Qualification added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveQualificationAsync(Guid teacherId, Guid qualificationId, CancellationToken cancellationToken = default)
        {
            var qualification = await _unitOfWork.Teachers.GetQualificationByIdAsync(qualificationId, cancellationToken);
            if (qualification == null || qualification.TeacherId != teacherId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Qualification was not found on this teacher.");
                return notFoundResponse;
            }

            _unitOfWork.Teachers.RemoveQualification(qualification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Qualification removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<TeacherQualificationDto>>> GetQualificationsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<List<TeacherQualificationDto>>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var qualifications = await _unitOfWork.Teachers.GetQualificationsAsync(teacherId, cancellationToken);

            var qualificationDtos = new List<TeacherQualificationDto>();
            foreach (var qualification in qualifications)
            {
                var qualificationDto = TeacherMapper.ToQualificationDto(qualification);
                qualificationDtos.Add(qualificationDto);
            }

            var successResponse = CommonResponse<List<TeacherQualificationDto>>.Success(qualificationDtos);
            return successResponse;
        }

        public async Task<CommonResponse<TeacherAssignmentDto>> AssignClassSubjectAsync(Guid teacherId, AssignTeacherCommand command, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(command.ClassSubjectId, cancellationToken);
            if (classSubject == null)
            {
                var subjectNotFoundResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.NotFound, "Class subject with id '" + command.ClassSubjectId + "' was not found.");
                return subjectNotFoundResponse;
            }

            // The assignment's section is DERIVED from the subject whenever the subject is
            // itself section-scoped, rather than left for the caller to (possibly wrongly)
            // repeat: a section-scoped subject can only ever be taught in its own section, so
            // there is exactly one valid value and the caller doesn't need to supply it (the old
            // code only rejected a *different* section, silently accepting a null one, which
            // left the assignment looking like "every section" for a subject that isn't offered
            // everywhere). A class-wide subject leaves the section optional as before -- null
            // covers every section, or one specific section for a per-section teacher split.
            Guid? effectiveClassSectionId = command.ClassSectionId;
            ClassSection classSection = null;
            if (classSubject.ClassSectionId.HasValue)
            {
                if (command.ClassSectionId.HasValue && command.ClassSectionId.Value != classSubject.ClassSectionId.Value)
                {
                    var scopedMismatchResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.ValidationError, "Subject '" + classSubject.SubjectCode + "' is only offered in a different section.");
                    return scopedMismatchResponse;
                }

                effectiveClassSectionId = classSubject.ClassSectionId;
                classSection = classSubject.ClassSection;
            }
            else if (command.ClassSectionId.HasValue)
            {
                classSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.ClassSectionId.Value, cancellationToken);
                if (classSection == null)
                {
                    var sectionNotFoundResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.NotFound, "Class section with id '" + command.ClassSectionId.Value + "' was not found.");
                    return sectionNotFoundResponse;
                }

                if (classSection.AcademicClassId != classSubject.AcademicClassId)
                {
                    var sectionMismatchResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.ValidationError, "That section belongs to a different class than the subject.");
                    return sectionMismatchResponse;
                }
            }

            var assignmentExists = await _unitOfWork.Teachers.AssignmentExistsAsync(teacherId, command.ClassSubjectId, effectiveClassSectionId, cancellationToken);
            if (assignmentExists)
            {
                var conflictResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.Conflict, "This teacher is already assigned to that class subject" + (effectiveClassSectionId.HasValue ? " for that section." : "."));
                return conflictResponse;
            }

            // A class teacher belongs to exactly one section, and a section has at most one.
            if (command.IsClassTeacher)
            {
                if (!effectiveClassSectionId.HasValue)
                {
                    var sectionRequiredResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.ValidationError, "ClassSectionId is required when IsClassTeacher is true -- a class teacher is assigned to one section.");
                    return sectionRequiredResponse;
                }

                var classTeacherExists = await _unitOfWork.Teachers.ClassTeacherExistsForSectionAsync(effectiveClassSectionId.Value, cancellationToken);
                if (classTeacherExists)
                {
                    var classTeacherConflictResponse = CommonResponse<TeacherAssignmentDto>.Fail(ResponseCodes.Conflict, "This section already has a class teacher. Remove that assignment first.");
                    return classTeacherConflictResponse;
                }
            }

            var assignment = new TeacherAssignment
            {
                TeacherId = teacherId,
                ClassSubjectId = command.ClassSubjectId,
                ClassSectionId = effectiveClassSectionId,
                IsClassTeacher = command.IsClassTeacher,
                ClassSubject = classSubject,
                ClassSection = classSection
            };

            await _unitOfWork.Teachers.AddAssignmentAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var assignmentDto = TeacherMapper.ToAssignmentDto(assignment);
            var successResponse = CommonResponse<TeacherAssignmentDto>.Success(assignmentDto, "Teacher assigned successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveAssignmentAsync(Guid teacherId, Guid assignmentId, CancellationToken cancellationToken = default)
        {
            var assignment = await _unitOfWork.Teachers.GetAssignmentByIdAsync(assignmentId, cancellationToken);
            if (assignment == null || assignment.TeacherId != teacherId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Assignment was not found on this teacher.");
                return notFoundResponse;
            }

            _unitOfWork.Teachers.RemoveAssignment(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Assignment removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<TeacherAssignmentDto>>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<List<TeacherAssignmentDto>>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var assignments = await _unitOfWork.Teachers.GetAssignmentsAsync(teacherId, cancellationToken);

            var assignmentDtos = new List<TeacherAssignmentDto>();
            foreach (var assignment in assignments)
            {
                var assignmentDto = TeacherMapper.ToAssignmentDto(assignment);
                assignmentDtos.Add(assignmentDto);
            }

            var successResponse = CommonResponse<List<TeacherAssignmentDto>>.Success(assignmentDtos);
            return successResponse;
        }

        public async Task<CommonResponse<TeacherDocumentDto>> UploadDocumentAsync(Guid teacherId, UploadTeacherDocumentCommand command, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default)
        {
            var validationResult = _uploadDocumentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TeacherDocumentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            if (fileContent == null || fileSizeBytes <= 0)
            {
                var noFileResponse = CommonResponse<TeacherDocumentDto>.Fail(ResponseCodes.ValidationError, "A document file is required.");
                return noFileResponse;
            }

            if (!DocumentFileRules.IsAllowedExtension(originalFileName))
            {
                var extensionResponse = CommonResponse<TeacherDocumentDto>.Fail(ResponseCodes.ValidationError, "Unsupported file type. Allowed: " + DocumentFileRules.AllowedExtensionsDisplay() + ".");
                return extensionResponse;
            }

            if (fileSizeBytes > DocumentFileRules.MaxFileSizeBytes)
            {
                var sizeResponse = CommonResponse<TeacherDocumentDto>.Fail(ResponseCodes.ValidationError, "File exceeds the maximum size of " + (DocumentFileRules.MaxFileSizeBytes / (1024 * 1024)) + " MB.");
                return sizeResponse;
            }

            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<TeacherDocumentDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var documentTypeCode = command.DocumentTypeCode.Trim();
            var typeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.DocumentType, documentTypeCode, cancellationToken);
            if (!typeExists)
            {
                var typeInvalidResponse = CommonResponse<TeacherDocumentDto>.Fail(ResponseCodes.ValidationError, "DocumentTypeCode '" + documentTypeCode + "' is not a known document type option.");
                return typeInvalidResponse;
            }

            var storedPath = await _fileStorage.SaveAsync(fileContent, originalFileName, "teacher-documents/" + teacherId, cancellationToken);

            var document = new TeacherDocument
            {
                TeacherId = teacherId,
                DocumentTypeCode = documentTypeCode,
                DocumentName = command.DocumentName.Trim(),
                FileName = originalFileName,
                FilePath = storedPath,
                ContentType = contentType,
                FileSizeBytes = fileSizeBytes,
                ValidUntil = command.ValidUntil,
                Remarks = command.Remarks?.Trim()
            };

            await _unitOfWork.Teachers.AddDocumentAsync(document, cancellationToken);
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

            var documentDto = TeacherMapper.ToDocumentDto(document);
            var successResponse = CommonResponse<TeacherDocumentDto>.Success(documentDto, "Document uploaded successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<TeacherDocumentDto>>> GetDocumentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<List<TeacherDocumentDto>>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var documents = await _unitOfWork.Teachers.GetDocumentsAsync(teacherId, cancellationToken);

            var documentDtos = new List<TeacherDocumentDto>();
            foreach (var document in documents)
            {
                var documentDto = TeacherMapper.ToDocumentDto(document);
                documentDtos.Add(documentDto);
            }

            var successResponse = CommonResponse<List<TeacherDocumentDto>>.Success(documentDtos);
            return successResponse;
        }

        public async Task<CommonResponse<TeacherDocumentFileDto>> GetDocumentFileAsync(Guid teacherId, Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await _unitOfWork.Teachers.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null || document.TeacherId != teacherId)
            {
                var notFoundResponse = CommonResponse<TeacherDocumentFileDto>.Fail(ResponseCodes.NotFound, "Document was not found on this teacher.");
                return notFoundResponse;
            }

            var contentStream = await _fileStorage.OpenReadAsync(document.FilePath, cancellationToken);
            if (contentStream == null)
            {
                var fileMissingResponse = CommonResponse<TeacherDocumentFileDto>.Fail(ResponseCodes.NotFound, "The stored file for this document is missing.");
                return fileMissingResponse;
            }

            var fileDto = new TeacherDocumentFileDto
            {
                Content = contentStream,
                ContentType = string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType,
                FileName = document.FileName
            };

            var successResponse = CommonResponse<TeacherDocumentFileDto>.Success(fileDto);
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteDocumentAsync(Guid teacherId, Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await _unitOfWork.Teachers.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null || document.TeacherId != teacherId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Document was not found on this teacher.");
                return notFoundResponse;
            }

            _unitOfWork.Teachers.RemoveDocument(document);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Only after the row is gone -- a failed save must not strand a row pointing at a
            // deleted file. Best-effort by contract.
            _fileStorage.Delete(document.FilePath);

            var successResponse = CommonResponse<bool>.Success(true, "Document deleted successfully.");
            return successResponse;
        }

        // The following three are thin convenience aliases over IEmployeeService's salary
        // machinery -- a Teacher's Id IS its Employee's Id (shared-PK pattern), so these just
        // forward. Kept so existing /api/teachers/{id}/salaries consumers don't break.

        public Task<CommonResponse<EmployeeSalaryDto>> AddSalaryAsync(Guid teacherId, AddEmployeeSalaryCommand command, CancellationToken cancellationToken = default)
        {
            return _employeeService.AddSalaryAsync(teacherId, command, cancellationToken);
        }

        public Task<CommonResponse<List<EmployeeSalaryDto>>> GetSalaryHistoryAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetSalaryHistoryAsync(teacherId, cancellationToken);
        }

        public Task<CommonResponse<EmployeeTaxCalculationDto>> GetCurrentSalaryTaxCalculationAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetCurrentSalaryTaxCalculationAsync(teacherId, fiscalYearId, cancellationToken);
        }

        public Task<CommonResponse<EmployeeMonthlyTaxBreakdownDto>> GetMonthlySalaryTaxCalculationAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetMonthlySalaryTaxCalculationAsync(teacherId, fiscalYearId, cancellationToken);
        }

        public Task<CommonResponse<DocumentPreviewDto>> GetPayslipPreviewAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetPayslipPreviewAsync(teacherId, fiscalYearId, cancellationToken);
        }

        public Task<CommonResponse<List<PayslipSummaryDto>>> GetPayslipsAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetPayslipsAsync(teacherId, fiscalYearId, cancellationToken);
        }

        public Task<CommonResponse<PayslipDetailDto>> GetPayslipDetailAsync(Guid teacherId, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetPayslipDetailAsync(teacherId, fiscalYearId, monthIndex, cancellationToken);
        }

        public Task<CommonResponse<EmployeeLoanDto>> RequestLoanAsync(Guid teacherId, RequestLoanCommand command, CancellationToken cancellationToken = default)
        {
            return _employeeService.RequestLoanAsync(teacherId, command, cancellationToken);
        }

        public Task<CommonResponse<List<EmployeeLoanDto>>> GetLoansAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetLoansAsync(teacherId, cancellationToken);
        }

        public Task<CommonResponse<EmployeeLoanDto>> ApproveLoanAsync(Guid teacherId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default)
        {
            return _employeeService.ApproveLoanAsync(teacherId, loanId, command, cancellationToken);
        }

        public Task<CommonResponse<EmployeeLoanDto>> RejectLoanAsync(Guid teacherId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default)
        {
            return _employeeService.RejectLoanAsync(teacherId, loanId, command, cancellationToken);
        }

        public Task<CommonResponse<EmployeeLoanDto>> CancelLoanAsync(Guid teacherId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default)
        {
            return _employeeService.CancelLoanAsync(teacherId, loanId, command, cancellationToken);
        }

        public async Task<CommonResponse<DocumentPreviewDto>> GetIdCardPreviewAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdWithEmployeeAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByTemplateTypeAsync(DocumentTemplateType.TeacherIdCard, cancellationToken);
            if (documentTemplate == null)
            {
                var noTemplateResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "No document template is configured for '" + DocumentTemplateType.TeacherIdCard + "' yet.");
                return noTemplateResponse;
            }

            var teacherDto = TeacherMapper.ToDto(teacher);

            var placeholderValues = new Dictionary<string, string>
            {
                { "EmployeeCode", teacherDto.EmployeeCode },
                { "TeacherName", BuildFullName(teacherDto.FirstName, teacherDto.MiddleName, teacherDto.LastName) },
                { "JobPositionCode", teacherDto.JobPositionCode },
                { "TeachingLicenseNo", teacherDto.TeachingLicenseNo },
                { "Specialization", teacherDto.Specialization },
                { "JoinDate", teacherDto.JoinDate.HasValue ? teacherDto.JoinDate.Value.ToString("yyyy-MM-dd") : string.Empty },
                { "Phone", teacherDto.Phone },
                { "Email", teacherDto.Email }
            };

            var renderedHtml = TemplateRenderer.Render(documentTemplate.HtmlContent, placeholderValues);

            var documentPreviewDto = new DocumentPreviewDto
            {
                TemplateType = DocumentTemplateType.TeacherIdCard,
                Html = renderedHtml
            };

            var successResponse = CommonResponse<DocumentPreviewDto>.Success(documentPreviewDto);
            return successResponse;
        }

        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                nameParts.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                nameParts.Add(middleName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                nameParts.Add(lastName);
            }

            var fullName = string.Join(" ", nameParts);
            return fullName;
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
