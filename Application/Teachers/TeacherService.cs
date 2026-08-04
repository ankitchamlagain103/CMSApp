using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
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
        private readonly IEmployeeService _employeeService;
        private readonly CreateTeacherCommandValidator _createValidator;
        private readonly UpdateTeacherCommandValidator _updateValidator;

        public TeacherService(
            IUnitOfWork unitOfWork,
            IEmployeeService employeeService,
            CreateTeacherCommandValidator createValidator,
            UpdateTeacherCommandValidator updateValidator)
        {
            _unitOfWork = unitOfWork;
            _employeeService = employeeService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
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

            var (assignment, errorCode, errorMessage) = await TeacherAssignmentBuilder.BuildAsync(_unitOfWork, teacherId, classSubject, command.ClassSectionId, command.IsClassTeacher, command.TimePeriodId, cancellationToken);
            if (errorCode != null)
            {
                var errorResponse = CommonResponse<TeacherAssignmentDto>.Fail(errorCode, errorMessage);
                return errorResponse;
            }

            await _unitOfWork.Teachers.AddAssignmentAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var assignmentDto = TeacherMapper.ToAssignmentDto(assignment);
            var successResponse = CommonResponse<TeacherAssignmentDto>.Success(assignmentDto, "Teacher assigned successfully.");
            return successResponse;
        }

        // Optimized multi-section counterpart to AssignClassSubjectAsync (2026-08-03) -- assigns
        // the same ClassSubject/TimePeriodId to a teacher across several sections in one call
        // instead of repeating the whole single-assignment flow once per section. A teacher can
        // freely be the class teacher of one section while also teaching this (or another)
        // subject in a different section -- that's just two separate TeacherAssignment rows, one
        // per section, exactly what this endpoint is built to create together.
        public async Task<CommonResponse<TeacherAssignmentBulkResultDto>> AssignClassSubjectBulkAsync(Guid teacherId, AssignTeacherBulkCommand command, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<TeacherAssignmentBulkResultDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            var requestedSectionIds = command.ClassSectionIds == null ? new List<Guid>() : command.ClassSectionIds.Distinct().ToList();
            if (requestedSectionIds.Count == 0)
            {
                var noSectionsResponse = CommonResponse<TeacherAssignmentBulkResultDto>.Fail(ResponseCodes.ValidationError, "At least one ClassSectionId is required -- use the single-assignment endpoint with a null ClassSectionId to cover every section instead.");
                return noSectionsResponse;
            }

            // A class teacher belongs to exactly one section -- rejected upfront as a request-shape
            // error rather than per-item, since "class teacher of 3 sections at once" isn't a
            // business conflict to skip past, it's a malformed request.
            if (command.IsClassTeacher && requestedSectionIds.Count != 1)
            {
                var classTeacherShapeResponse = CommonResponse<TeacherAssignmentBulkResultDto>.Fail(ResponseCodes.ValidationError, "IsClassTeacher can only be set when assigning exactly one section.");
                return classTeacherShapeResponse;
            }

            var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(command.ClassSubjectId, cancellationToken);
            if (classSubject == null)
            {
                var subjectNotFoundResponse = CommonResponse<TeacherAssignmentBulkResultDto>.Fail(ResponseCodes.NotFound, "Class subject with id '" + command.ClassSubjectId + "' was not found.");
                return subjectNotFoundResponse;
            }

            var created = new List<TeacherAssignmentDto>();
            var skipped = new List<TeacherAssignmentSkipDto>();

            // TimePeriodId is fixed for the whole call -- if it's set, every section beyond the
            // first one is now a time-period conflict with the ones already staged (a teacher
            // can't teach several different sections during the identical period), since
            // TeacherHasTimePeriodConflictAsync only sees rows already committed to the
            // database, not ones Added-but-not-yet-SaveChanges'd within this same loop. Callers
            // wanting several sections at genuinely different periods should use
            // .../assignments/bulk-entry instead, where each row names its own TimePeriodId.
            var timePeriodStaged = false;
            foreach (var sectionId in requestedSectionIds)
            {
                if (command.TimePeriodId.HasValue && timePeriodStaged)
                {
                    var conflictSkip = new TeacherAssignmentSkipDto
                    {
                        ClassSectionId = sectionId,
                        Reason = "Another section earlier in this same request already assigns this teacher to that time period -- a teacher can't teach two sections during the same period."
                    };
                    skipped.Add(conflictSkip);
                    continue;
                }

                var (assignment, errorCode, errorMessage) = await TeacherAssignmentBuilder.BuildAsync(_unitOfWork, teacherId, classSubject, sectionId, command.IsClassTeacher, command.TimePeriodId, cancellationToken);
                if (errorCode != null)
                {
                    var skip = new TeacherAssignmentSkipDto
                    {
                        ClassSectionId = sectionId,
                        Reason = errorMessage
                    };
                    skipped.Add(skip);
                    continue;
                }

                if (command.TimePeriodId.HasValue)
                {
                    timePeriodStaged = true;
                }

                await _unitOfWork.Teachers.AddAssignmentAsync(assignment, cancellationToken);
                created.Add(TeacherMapper.ToAssignmentDto(assignment));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = new TeacherAssignmentBulkResultDto
            {
                Created = created,
                Skipped = skipped
            };
            var successResponse = CommonResponse<TeacherAssignmentBulkResultDto>.Success(resultDto, created.Count + " assignment(s) created, " + skipped.Count + " skipped.");
            return successResponse;
        }

        // General bulk-entry counterpart to AssignClassSubjectBulkAsync -- that endpoint fixes
        // ClassSubjectId/TimePeriodId for the whole call and only varies the section list; this
        // one lets each row name its own ClassSubjectId/ClassSectionId/TimePeriodId, so a
        // teacher's whole routine (several different classes/subjects/sections/periods) can be
        // entered in one submission. Skip-list style, like every other bulk endpoint in this
        // codebase -- a bad row is reported in Skipped, it never fails the whole request.
        public async Task<CommonResponse<TeacherAssignmentBulkEntryResultDto>> AssignClassSubjectBulkEntryAsync(Guid teacherId, AssignTeacherBulkEntryCommand command, CancellationToken cancellationToken = default)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId, cancellationToken);
            if (teacher == null)
            {
                var notFoundResponse = CommonResponse<TeacherAssignmentBulkEntryResultDto>.Fail(ResponseCodes.NotFound, "Teacher with id '" + teacherId + "' was not found.");
                return notFoundResponse;
            }

            if (command.Items == null || command.Items.Count == 0)
            {
                var noItemsResponse = CommonResponse<TeacherAssignmentBulkEntryResultDto>.Fail(ResponseCodes.ValidationError, "At least one item is required.");
                return noItemsResponse;
            }

            var created = new List<TeacherAssignmentDto>();
            var skipped = new List<TeacherAssignmentEntrySkipDto>();
            var classSubjectCache = new Dictionary<Guid, ClassSubject>();

            // Tracks what's already been staged earlier in this same request -- AssignmentExistsAsync
            // and ClassTeacherExistsForSectionAsync (called from TeacherAssignmentBuilder.BuildAsync)
            // only see rows already committed to the database, not entities Added-but-not-yet-
            // SaveChanges'd, so two rows in the same batch that collide with each other (not with
            // existing data) would otherwise both pass those checks and hit a unique-index violation
            // at save time instead.
            var stagedKeys = new HashSet<(Guid ClassSubjectId, Guid ClassSectionKey)>();
            var stagedClassTeacherSections = new HashSet<Guid>();

            // Same reasoning as above -- TeacherHasTimePeriodConflictAsync (called from
            // TeacherAssignmentBuilder.BuildAsync) only sees rows already committed to the
            // database, so two rows in this same batch naming the same TimePeriodId for this
            // (fixed, route-scoped) teacher must be caught here.
            var stagedTimePeriods = new HashSet<Guid>();

            for (var itemIndex = 0; itemIndex < command.Items.Count; itemIndex++)
            {
                var item = command.Items[itemIndex];

                if (item.ClassSubjectId == Guid.Empty)
                {
                    var skip = new TeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "ClassSubjectId is required."
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
                    var skip = new TeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Class subject with id '" + item.ClassSubjectId + "' was not found."
                    };
                    skipped.Add(skip);
                    continue;
                }

                if (item.TimePeriodId.HasValue && stagedTimePeriods.Contains(item.TimePeriodId.Value))
                {
                    var skip = new TeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Another item earlier in this same request already assigns this teacher to that time period."
                    };
                    skipped.Add(skip);
                    continue;
                }

                var (assignment, errorCode, errorMessage) = await TeacherAssignmentBuilder.BuildAsync(_unitOfWork, teacherId, classSubject, item.ClassSectionId, item.IsClassTeacher, item.TimePeriodId, cancellationToken);
                if (errorCode != null)
                {
                    var skip = new TeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = errorMessage
                    };
                    skipped.Add(skip);
                    continue;
                }

                var sectionKey = assignment.ClassSectionId.HasValue ? assignment.ClassSectionId.Value : Guid.Empty;
                var stagedKey = (assignment.ClassSubjectId, sectionKey);
                if (stagedKeys.Contains(stagedKey))
                {
                    var skip = new TeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Duplicate of an earlier item in this same request."
                    };
                    skipped.Add(skip);
                    continue;
                }

                if (assignment.IsClassTeacher && stagedClassTeacherSections.Contains(assignment.ClassSectionId.Value))
                {
                    var skip = new TeacherAssignmentEntrySkipDto
                    {
                        ItemIndex = itemIndex,
                        ClassSubjectId = item.ClassSubjectId,
                        ClassSectionId = item.ClassSectionId,
                        Reason = "Another item earlier in this same request already makes this teacher the class teacher for that section."
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
                    stagedTimePeriods.Add(assignment.TimePeriodId.Value);
                }

                stagedKeys.Add(stagedKey);
                await _unitOfWork.Teachers.AddAssignmentAsync(assignment, cancellationToken);
                created.Add(TeacherMapper.ToAssignmentDto(assignment));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = new TeacherAssignmentBulkEntryResultDto
            {
                Created = created,
                Skipped = skipped
            };
            var successResponse = CommonResponse<TeacherAssignmentBulkEntryResultDto>.Success(resultDto, created.Count + " assignment(s) created, " + skipped.Count + " skipped.");
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

        public Task<CommonResponse<TaxPlanningDto>> GetTaxPlanningAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetTaxPlanningAsync(teacherId, fiscalYearId, cancellationToken);
        }

        public Task<CommonResponse<SalaryAnnualForecastDto>> GetAnnualForecastAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetAnnualForecastAsync(teacherId, fiscalYearId, cancellationToken);
        }

        public Task<CommonResponse<TaxDetailsGridDto>> GetTaxDetailsGridAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetTaxDetailsGridAsync(teacherId, fiscalYearId, cancellationToken);
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

        public Task<CommonResponse<SalaryForecastDto>> GetSalaryForecastAsync(Guid teacherId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            return _employeeService.GetSalaryForecastAsync(teacherId, fiscalYearId, cancellationToken);
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
