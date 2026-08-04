using Application.Calendars;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Exams.Commands;
using Application.Exams.Dtos;
using Application.Exams.Validators;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Exams
{
    public class ExamService : IExamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBsAdConversionService _conversionService;
        private readonly CreateExamTermCommandValidator _createExamTermValidator;
        private readonly UpdateExamTermCommandValidator _updateExamTermValidator;
        private readonly CreateExamCommandValidator _createExamValidator;
        private readonly UpdateExamCommandValidator _updateExamValidator;
        private readonly SaveExamRoutineCommandValidator _saveExamRoutineValidator;
        private readonly CreateStudentExamMarkCommandValidator _createMarkValidator;
        private readonly UpdateStudentExamMarkCommandValidator _updateMarkValidator;
        private readonly BulkUpsertStudentExamMarksCommandValidator _bulkUpsertMarksValidator;
        private readonly GenerateExamResultsCommandValidator _generateResultsValidator;
        private readonly WithholdExamResultCommandValidator _withholdResultValidator;

        public ExamService(
            IUnitOfWork unitOfWork,
            IBsAdConversionService conversionService,
            CreateExamTermCommandValidator createExamTermValidator,
            UpdateExamTermCommandValidator updateExamTermValidator,
            CreateExamCommandValidator createExamValidator,
            UpdateExamCommandValidator updateExamValidator,
            SaveExamRoutineCommandValidator saveExamRoutineValidator,
            CreateStudentExamMarkCommandValidator createMarkValidator,
            UpdateStudentExamMarkCommandValidator updateMarkValidator,
            BulkUpsertStudentExamMarksCommandValidator bulkUpsertMarksValidator,
            GenerateExamResultsCommandValidator generateResultsValidator,
            WithholdExamResultCommandValidator withholdResultValidator)
        {
            _unitOfWork = unitOfWork;
            _conversionService = conversionService;
            _createExamTermValidator = createExamTermValidator;
            _updateExamTermValidator = updateExamTermValidator;
            _createExamValidator = createExamValidator;
            _updateExamValidator = updateExamValidator;
            _saveExamRoutineValidator = saveExamRoutineValidator;
            _createMarkValidator = createMarkValidator;
            _updateMarkValidator = updateMarkValidator;
            _bulkUpsertMarksValidator = bulkUpsertMarksValidator;
            _generateResultsValidator = generateResultsValidator;
            _withholdResultValidator = withholdResultValidator;
        }

        // --- Exam terms ---

        public async Task<CommonResponse<ExamTermDto>> CreateExamTermAsync(CreateExamTermCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createExamTermValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ExamTermDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(command.AcademicYearId, cancellationToken);
            if (academicYear == null)
            {
                var yearNotFoundResponse = CommonResponse<ExamTermDto>.Fail(ResponseCodes.NotFound, "Academic year with id '" + command.AcademicYearId + "' was not found.");
                return yearNotFoundResponse;
            }

            var code = command.Code.Trim();
            var codeExists = await _unitOfWork.ExamTerms.CodeExistsAsync(code, null, cancellationToken);
            if (codeExists)
            {
                var conflictResponse = CommonResponse<ExamTermDto>.Fail(ResponseCodes.Conflict, "Exam term code '" + code + "' is already in use (possibly by a soft-deleted term).");
                return conflictResponse;
            }

            var examTerm = new ExamTerm
            {
                AcademicYearId = command.AcademicYearId,
                Code = code,
                Name = command.Name.Trim(),
                Sequence = command.Sequence,
                StartDate = command.StartDate.Date,
                EndDate = command.EndDate.Date,
                PublishResult = command.PublishResult,
                Status = command.Status
            };

            await _unitOfWork.ExamTerms.AddAsync(examTerm, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var examTermDto = ExamMapper.ToExamTermDto(examTerm);
            var successResponse = CommonResponse<ExamTermDto>.Success(examTermDto, "Exam term created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ExamTermDto>> GetExamTermByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(id, cancellationToken);
            if (examTerm == null)
            {
                var notFoundResponse = CommonResponse<ExamTermDto>.Fail(ResponseCodes.NotFound, "Exam term with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var examTermDto = ExamMapper.ToExamTermDto(examTerm);
            var successResponse = CommonResponse<ExamTermDto>.Success(examTermDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<ExamTermDto>>> GetExamTermsAsync(Guid? academicYearId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedTerms = await _unitOfWork.ExamTerms.GetPagedByFilterAsync(academicYearId, page, pageSize, cancellationToken);

            var examTermDtos = new List<ExamTermDto>();
            foreach (var examTerm in pagedTerms.Items)
            {
                var examTermDto = ExamMapper.ToExamTermDto(examTerm);
                examTermDtos.Add(examTermDto);
            }

            var paginatedResponse = new PaginatedResponse<ExamTermDto>
            {
                Items = examTermDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedTerms.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<ExamTermDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<ExamTermDto>> UpdateExamTermAsync(Guid id, UpdateExamTermCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateExamTermValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ExamTermDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(id, cancellationToken);
            if (examTerm == null)
            {
                var notFoundResponse = CommonResponse<ExamTermDto>.Fail(ResponseCodes.NotFound, "Exam term with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            examTerm.Name = command.Name.Trim();
            examTerm.Sequence = command.Sequence;
            examTerm.StartDate = command.StartDate.Date;
            examTerm.EndDate = command.EndDate.Date;
            examTerm.PublishResult = command.PublishResult;
            examTerm.Status = command.Status;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var examTermDto = ExamMapper.ToExamTermDto(examTerm);
            var successResponse = CommonResponse<ExamTermDto>.Success(examTermDto, "Exam term updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteExamTermAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(id, cancellationToken);
            if (examTerm == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Exam term with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var hasExams = await _unitOfWork.ExamTerms.HasExamsAsync(id, cancellationToken);
            if (hasExams)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This exam term still has exams. Delete them first.");
                return conflictResponse;
            }

            _unitOfWork.ExamTerms.Remove(examTerm);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Exam term deleted successfully.");
            return successResponse;
        }

        // --- Exams (one subject's sitting for one section within an exam term) ---

        public async Task<CommonResponse<ExamDto>> CreateExamAsync(CreateExamCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createExamValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(command.ExamTermId, cancellationToken);
            if (examTerm == null)
            {
                var termNotFoundResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.NotFound, "Exam term with id '" + command.ExamTermId + "' was not found.");
                return termNotFoundResponse;
            }

            var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(command.ClassSubjectId, cancellationToken);
            if (classSubject == null)
            {
                var subjectNotFoundResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.NotFound, "Class subject with id '" + command.ClassSubjectId + "' was not found.");
                return subjectNotFoundResponse;
            }

            var alreadyExists = await _unitOfWork.ExamTerms.ExamExistsAsync(command.ExamTermId, command.ClassSubjectId, null, cancellationToken);
            if (alreadyExists)
            {
                var conflictResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.Conflict, "An exam for this subject already exists within this term.");
                return conflictResponse;
            }

            var (resolvedStartTime, resolvedEndTime, resolvedTimePeriod, timeIssue) = await ResolveExamTimesAsync(classSubject.AcademicClassId, command.TimePeriodId, command.StartTime, command.EndTime, cancellationToken);
            if (timeIssue != null)
            {
                var timeIssueResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.ValidationError, timeIssue);
                return timeIssueResponse;
            }

            var examDate = command.ExamDate.Date;
            CalendarEvent calendarEvent;
            try
            {
                calendarEvent = await BuildExamCalendarEventAsync(examTerm.Name, classSubject.SubjectCode, examDate, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var calendarFailureResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return calendarFailureResponse;
            }

            await _unitOfWork.CalendarEvents.AddAsync(calendarEvent, cancellationToken);

            var exam = new Exam
            {
                ExamTermId = command.ExamTermId,
                ClassSubjectId = command.ClassSubjectId,
                ExamDate = examDate,
                TimePeriodId = command.TimePeriodId,
                StartTime = resolvedStartTime,
                EndTime = resolvedEndTime,
                Remarks = command.Remarks,
                CalendarEventId = calendarEvent.Id,
                ClassSubject = classSubject,
                TimePeriod = resolvedTimePeriod
            };

            await _unitOfWork.ExamTerms.AddExamAsync(exam, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var examDto = ExamMapper.ToExamDto(exam);
            var successResponse = CommonResponse<ExamDto>.Success(examDto, "Exam created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<SaveExamRoutineResultDto>> SaveExamRoutineAsync(SaveExamRoutineCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _saveExamRoutineValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SaveExamRoutineResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(command.ExamTermId, cancellationToken);
            if (examTerm == null)
            {
                var termNotFoundResponse = CommonResponse<SaveExamRoutineResultDto>.Fail(ResponseCodes.NotFound, "Exam term with id '" + command.ExamTermId + "' was not found.");
                return termNotFoundResponse;
            }

            var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(command.AcademicClassId, cancellationToken);
            if (academicClass == null)
            {
                var classNotFoundResponse = CommonResponse<SaveExamRoutineResultDto>.Fail(ResponseCodes.NotFound, "Class with id '" + command.AcademicClassId + "' was not found.");
                return classNotFoundResponse;
            }

            var existingExams = await _unitOfWork.ExamTerms.GetExamsByTermAsync(command.ExamTermId, command.AcademicClassId, cancellationToken);
            var existingExamsBySubjectId = new Dictionary<Guid, Exam>();
            foreach (var existingExam in existingExams)
            {
                existingExamsBySubjectId[existingExam.ClassSubjectId] = existingExam;
            }

            // --- Pass 1: validate the complete timetable, collecting every issue before mutating
            // anything -- the save is all-or-nothing, so a single aggregated error beats failing
            // fast on the first problem found.
            var issues = new List<string>();
            var seenSubjectIds = new HashSet<Guid>();
            var resolvedItems = new List<(ExamRoutineItemInput Item, ClassSubject ClassSubject, TimeSpan StartTime, TimeSpan EndTime, TimePeriod TimePeriod)>();

            foreach (var item in command.Items)
            {
                if (!seenSubjectIds.Add(item.ClassSubjectId))
                {
                    issues.Add("Duplicate subject '" + item.ClassSubjectId + "' in the request.");
                    continue;
                }

                var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(item.ClassSubjectId, cancellationToken);
                if (classSubject == null || classSubject.AcademicClassId != command.AcademicClassId)
                {
                    issues.Add("Class subject with id '" + item.ClassSubjectId + "' was not found on this class.");
                    continue;
                }

                var examDate = item.ExamDate.Date;
                if (examDate < examTerm.StartDate.Date || examDate > examTerm.EndDate.Date)
                {
                    issues.Add("Exam date for subject '" + classSubject.SubjectCode + "' (" + examDate.ToString("yyyy-MM-dd") + ") falls outside the exam term's date range (" + examTerm.StartDate.ToString("yyyy-MM-dd") + " to " + examTerm.EndDate.ToString("yyyy-MM-dd") + ").");
                    continue;
                }

                var (resolvedStartTime, resolvedEndTime, resolvedTimePeriod, timeIssue) = await ResolveExamTimesAsync(command.AcademicClassId, item.TimePeriodId, item.StartTime, item.EndTime, cancellationToken);
                if (timeIssue != null)
                {
                    issues.Add("Subject '" + classSubject.SubjectCode + "': " + timeIssue);
                    continue;
                }

                try
                {
                    await _conversionService.ConvertAdToBsAsync(examDate, cancellationToken);
                }
                catch (BsCalendarException calendarException)
                {
                    issues.Add("Subject '" + classSubject.SubjectCode + "': " + calendarException.Message);
                    continue;
                }

                resolvedItems.Add((item, classSubject, resolvedStartTime, resolvedEndTime, resolvedTimePeriod));
            }

            // Student Overlap Check -- a class can't sit two subjects at overlapping times on the
            // same date. Pairwise, only among items that already passed the checks above.
            for (var i = 0; i < resolvedItems.Count; i++)
            {
                for (var j = i + 1; j < resolvedItems.Count; j++)
                {
                    var first = resolvedItems[i];
                    var second = resolvedItems[j];

                    if (first.Item.ExamDate.Date != second.Item.ExamDate.Date)
                    {
                        continue;
                    }

                    if (TimeRangesOverlap(first.StartTime, first.EndTime, second.StartTime, second.EndTime))
                    {
                        issues.Add("Subjects '" + first.ClassSubject.SubjectCode + "' and '" + second.ClassSubject.SubjectCode + "' overlap on " + first.Item.ExamDate.Date.ToString("yyyy-MM-dd") + ".");
                    }
                }
            }

            // An existing exam for this class/term whose subject is no longer in the submitted
            // routine is a removal -- allowed only while it has no recorded marks (per policy: a
            // removal that would discard graded work fails the whole save instead).
            var examsToRemove = new List<Exam>();
            foreach (var existingExam in existingExams)
            {
                if (seenSubjectIds.Contains(existingExam.ClassSubjectId))
                {
                    continue;
                }

                var hasMarks = await _unitOfWork.ExamTerms.HasMarksAsync(existingExam.Id, cancellationToken);
                if (hasMarks)
                {
                    var subjectCode = existingExam.ClassSubject != null ? existingExam.ClassSubject.SubjectCode : existingExam.ClassSubjectId.ToString();
                    issues.Add("Subject '" + subjectCode + "' is no longer in the submitted routine but already has recorded marks -- remove its marks first, or include it in the routine.");
                    continue;
                }

                examsToRemove.Add(existingExam);
            }

            if (issues.Count > 0)
            {
                var combinedIssues = string.Join(" ", issues);
                var issuesResponse = CommonResponse<SaveExamRoutineResultDto>.Fail(ResponseCodes.ValidationError, combinedIssues);
                return issuesResponse;
            }

            // --- Pass 2: every item validated -- mutate and save once, atomically. ---
            var resultDto = new SaveExamRoutineResultDto { ExamTermId = command.ExamTermId, AcademicClassId = command.AcademicClassId };

            foreach (var resolved in resolvedItems)
            {
                var item = resolved.Item;
                var classSubject = resolved.ClassSubject;
                var examDate = item.ExamDate.Date;

                if (existingExamsBySubjectId.TryGetValue(item.ClassSubjectId, out var existingExam))
                {
                    existingExam.ExamDate = examDate;
                    existingExam.TimePeriodId = item.TimePeriodId;
                    existingExam.TimePeriod = resolved.TimePeriod;
                    existingExam.StartTime = resolved.StartTime;
                    existingExam.EndTime = resolved.EndTime;
                    existingExam.Remarks = item.Remarks;

                    if (existingExam.CalendarEventId.HasValue)
                    {
                        var calendarEvent = await _unitOfWork.CalendarEvents.GetByIdAsync(existingExam.CalendarEventId.Value, cancellationToken);
                        if (calendarEvent != null)
                        {
                            var (bsYear, bsMonth, bsDay) = await _conversionService.ConvertAdToBsAsync(examDate, cancellationToken);
                            calendarEvent.Title = BuildExamEventTitle(examTerm.Name, classSubject.SubjectCode);
                            calendarEvent.AdDate = examDate;
                            calendarEvent.BsYear = bsYear;
                            calendarEvent.BsMonth = bsMonth;
                            calendarEvent.BsDay = bsDay;
                        }
                    }

                    resultDto.UpdatedCount++;
                    resultDto.Items.Add(ExamMapper.ToExamDto(existingExam));
                }
                else
                {
                    var calendarEvent = await BuildExamCalendarEventAsync(examTerm.Name, classSubject.SubjectCode, examDate, cancellationToken);
                    await _unitOfWork.CalendarEvents.AddAsync(calendarEvent, cancellationToken);

                    var newExam = new Exam
                    {
                        ExamTermId = command.ExamTermId,
                        ClassSubjectId = item.ClassSubjectId,
                        ExamDate = examDate,
                        TimePeriodId = item.TimePeriodId,
                        StartTime = resolved.StartTime,
                        EndTime = resolved.EndTime,
                        Remarks = item.Remarks,
                        CalendarEventId = calendarEvent.Id,
                        ClassSubject = classSubject,
                        TimePeriod = resolved.TimePeriod
                    };

                    await _unitOfWork.ExamTerms.AddExamAsync(newExam, cancellationToken);

                    resultDto.CreatedCount++;
                    resultDto.Items.Add(ExamMapper.ToExamDto(newExam));
                }
            }

            foreach (var examToRemove in examsToRemove)
            {
                if (examToRemove.CalendarEventId.HasValue)
                {
                    var calendarEvent = await _unitOfWork.CalendarEvents.GetByIdAsync(examToRemove.CalendarEventId.Value, cancellationToken);
                    if (calendarEvent != null)
                    {
                        _unitOfWork.CalendarEvents.Remove(calendarEvent);
                    }
                }

                _unitOfWork.ExamTerms.RemoveExam(examToRemove);
                resultDto.DeletedCount++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successMessage = "Exam routine saved: " + resultDto.CreatedCount + " created, " + resultDto.UpdatedCount + " updated, " + resultDto.DeletedCount + " removed.";
            var successResponse = CommonResponse<SaveExamRoutineResultDto>.Success(resultDto, successMessage);
            return successResponse;
        }

        private static bool TimeRangesOverlap(TimeSpan startA, TimeSpan endA, TimeSpan startB, TimeSpan endB)
        {
            return startA < endB && startB < endA;
        }

        // Class period timing (2026-07-30, moved off the Config catalog onto a real TimePeriod FK
        // 2026-08-03 -- see Domain/Entities/TimePeriod's doc comment for why): resolves the
        // concrete StartTime/EndTime an Exam is stored with, either from a picked TimePeriod (its
        // own StartTime/EndTime) or from raw times supplied directly. The command-level validator
        // already guarantees exactly one path was attempted; this is where the period path is
        // actually looked up. A picked period must (a) not be a Break and (b) be mapped, via
        // ClassTimePeriod, to the exam's own class -- the same two checks
        // TeacherService.BuildAssignmentAsync applies for TeacherAssignment.TimePeriodId. Issue is
        // non-null on any failure -- the caller returns it as a ValidationError without mutating
        // anything.
        private async Task<(TimeSpan StartTime, TimeSpan EndTime, TimePeriod TimePeriod, string Issue)> ResolveExamTimesAsync(Guid academicClassId, Guid? timePeriodId, TimeSpan? startTime, TimeSpan? endTime, CancellationToken cancellationToken)
        {
            TimeSpan resolvedStartTime;
            TimeSpan resolvedEndTime;
            TimePeriod resolvedTimePeriod = null;

            if (timePeriodId.HasValue)
            {
                var timePeriod = await _unitOfWork.TimePeriods.GetByIdAsync(timePeriodId.Value, cancellationToken);
                if (timePeriod == null)
                {
                    return (default, default, null, "Time period with id '" + timePeriodId.Value + "' was not found.");
                }

                if (timePeriod.Kind == PeriodKind.Break)
                {
                    return (default, default, null, "'" + timePeriod.Name + "' is a break, not a teaching period.");
                }

                var isMapped = await _unitOfWork.TimePeriods.IsMappedToClassAsync(academicClassId, timePeriodId.Value, cancellationToken);
                if (!isMapped)
                {
                    return (default, default, null, "'" + timePeriod.Name + "' is not mapped to this class -- map it first via the Time Periods screen.");
                }

                resolvedStartTime = timePeriod.StartTime;
                resolvedEndTime = timePeriod.EndTime;
                resolvedTimePeriod = timePeriod;
            }
            else
            {
                resolvedStartTime = startTime.Value;
                resolvedEndTime = endTime.Value;
            }

            if (resolvedEndTime <= resolvedStartTime)
            {
                return (default, default, null, "EndTime must be after StartTime.");
            }

            return (resolvedStartTime, resolvedEndTime, resolvedTimePeriod, null);
        }

        public async Task<CommonResponse<ExamDto>> GetExamByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(id, cancellationToken);
            if (exam == null)
            {
                var notFoundResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.NotFound, "Exam with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var examDto = ExamMapper.ToExamDto(exam);
            var successResponse = CommonResponse<ExamDto>.Success(examDto);
            return successResponse;
        }

        public async Task<CommonResponse<List<ExamDto>>> GetExamsAsync(Guid? examTermId, Guid? classSubjectId, Guid? teacherId, CancellationToken cancellationToken = default)
        {
            List<TeacherAssignment> teacherAssignments = null;
            if (teacherId.HasValue)
            {
                var assignments = await _unitOfWork.Teachers.GetAssignmentsAsync(teacherId.Value, cancellationToken);
                teacherAssignments = new List<TeacherAssignment>(assignments);

                if (teacherAssignments.Count == 0)
                {
                    var emptyResponse = CommonResponse<List<ExamDto>>.Success(new List<ExamDto>());
                    return emptyResponse;
                }
            }

            var exams = await _unitOfWork.ExamTerms.GetExamsAsync(examTermId, classSubjectId, cancellationToken);

            var examDtos = new List<ExamDto>();
            foreach (var exam in exams)
            {
                if (teacherAssignments != null && !IsTeacherAssignedToExam(teacherAssignments, exam))
                {
                    continue;
                }

                var examDto = ExamMapper.ToExamDto(exam);
                examDtos.Add(examDto);
            }

            var successResponse = CommonResponse<List<ExamDto>>.Success(examDtos);
            return successResponse;
        }

        // A teacher is responsible for an exam when they have a TeacherAssignment for the exam's
        // subject -- an Exam no longer has its own section, so there's nothing left to match
        // beyond the subject itself (an assignment scoped to one section still means "teaches
        // this subject", which is enough to grade it).
        private static bool IsTeacherAssignedToExam(List<TeacherAssignment> assignments, Exam exam)
        {
            foreach (var assignment in assignments)
            {
                if (assignment.ClassSubjectId == exam.ClassSubjectId)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<CommonResponse<ExamDto>> UpdateExamAsync(Guid id, UpdateExamCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateExamValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(id, cancellationToken);
            if (exam == null)
            {
                var notFoundResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.NotFound, "Exam with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var (resolvedStartTime, resolvedEndTime, resolvedTimePeriod, timeIssue) = await ResolveExamTimesAsync(exam.ClassSubject.AcademicClassId, command.TimePeriodId, command.StartTime, command.EndTime, cancellationToken);
            if (timeIssue != null)
            {
                var timeIssueResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.ValidationError, timeIssue);
                return timeIssueResponse;
            }

            var examDate = command.ExamDate.Date;
            (int BsYear, int BsMonth, int BsDay) bsDate;
            try
            {
                bsDate = await _conversionService.ConvertAdToBsAsync(examDate, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var calendarFailureResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return calendarFailureResponse;
            }

            exam.ExamDate = examDate;
            exam.TimePeriodId = command.TimePeriodId;
            exam.TimePeriod = resolvedTimePeriod;
            exam.StartTime = resolvedStartTime;
            exam.EndTime = resolvedEndTime;
            exam.Remarks = command.Remarks;

            if (exam.CalendarEventId.HasValue)
            {
                var calendarEvent = await _unitOfWork.CalendarEvents.GetByIdAsync(exam.CalendarEventId.Value, cancellationToken);
                if (calendarEvent != null)
                {
                    var examTermName = exam.ExamTerm != null ? exam.ExamTerm.Name : null;
                    var subjectCode = exam.ClassSubject != null ? exam.ClassSubject.SubjectCode : null;
                    calendarEvent.Title = BuildExamEventTitle(examTermName, subjectCode);
                    calendarEvent.AdDate = examDate;
                    calendarEvent.BsYear = bsDate.BsYear;
                    calendarEvent.BsMonth = bsDate.BsMonth;
                    calendarEvent.BsDay = bsDate.BsDay;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var examDto = ExamMapper.ToExamDto(exam);
            var successResponse = CommonResponse<ExamDto>.Success(examDto, "Exam updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteExamAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(id, cancellationToken);
            if (exam == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Exam with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var hasMarks = await _unitOfWork.ExamTerms.HasMarksAsync(id, cancellationToken);
            if (hasMarks)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This exam still has recorded marks. Delete them first.");
                return conflictResponse;
            }

            if (exam.CalendarEventId.HasValue)
            {
                var calendarEvent = await _unitOfWork.CalendarEvents.GetByIdAsync(exam.CalendarEventId.Value, cancellationToken);
                if (calendarEvent != null)
                {
                    _unitOfWork.CalendarEvents.Remove(calendarEvent);
                }
            }

            _unitOfWork.ExamTerms.RemoveExam(exam);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Exam deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<ExamDto>> LockExamAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(id, cancellationToken);
            if (exam == null)
            {
                var notFoundResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.NotFound, "Exam with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            exam.MarksLocked = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var examDto = ExamMapper.ToExamDto(exam);
            var successResponse = CommonResponse<ExamDto>.Success(examDto, "Exam locked; marks entry is now closed.");
            return successResponse;
        }

        public async Task<CommonResponse<ExamDto>> UnlockExamAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(id, cancellationToken);
            if (exam == null)
            {
                var notFoundResponse = CommonResponse<ExamDto>.Fail(ResponseCodes.NotFound, "Exam with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            exam.MarksLocked = false;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var examDto = ExamMapper.ToExamDto(exam);
            var successResponse = CommonResponse<ExamDto>.Success(examDto, "Exam unlocked; marks entry is open again.");
            return successResponse;
        }

        // --- Marks Entry ---

        public async Task<CommonResponse<StudentExamMarkDto>> CreateStudentExamMarkAsync(CreateStudentExamMarkCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createMarkValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(command.ExamId, cancellationToken);
            if (exam == null)
            {
                var examNotFoundResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.NotFound, "Exam with id '" + command.ExamId + "' was not found.");
                return examNotFoundResponse;
            }

            if (exam.MarksLocked)
            {
                var lockedResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.ValidationError, "Marks entry is locked for this exam.");
                return lockedResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(command.EnrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var enrollmentNotFoundResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + command.EnrollmentId + "' was not found.");
                return enrollmentNotFoundResponse;
            }

            var eligibleEnrollments = await EligibleEnrollmentResolver.ResolveAsync(_unitOfWork, exam.ClassSubject, exam.ExamTerm.AcademicYearId, null, cancellationToken);
            var isEligible = false;
            foreach (var eligibleEnrollment in eligibleEnrollments)
            {
                if (eligibleEnrollment.Id == enrollment.Id)
                {
                    isEligible = true;
                    break;
                }
            }

            if (!isEligible)
            {
                var notEligibleResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.ValidationError, "This enrollment is not eligible for this exam's subject.");
                return notEligibleResponse;
            }

            var existingMark = await _unitOfWork.ExamTerms.GetMarkByExamAndEnrollmentAsync(command.ExamId, command.EnrollmentId, cancellationToken);
            if (existingMark != null)
            {
                var conflictResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.Conflict, "Marks for this student on this exam are already recorded. Use update instead.");
                return conflictResponse;
            }

            var subjectValidationError = ValidateMarkAgainstSubject(exam.ClassSubject, command.TheoryObtainedMarks, command.PracticalObtainedMarks, command.TheoryGraceMarks, command.PracticalGraceMarks, command.TheoryAbsent, command.PracticalAbsent);
            if (subjectValidationError != null)
            {
                var subjectValidationResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.ValidationError, subjectValidationError);
                return subjectValidationResponse;
            }

            var totals = ComputeMarkTotals(exam.ClassSubject, command.TheoryObtainedMarks, command.PracticalObtainedMarks, command.InternalMarks, command.TheoryGraceMarks, command.PracticalGraceMarks, command.TheoryAbsent, command.PracticalAbsent);

            var mark = new StudentExamMark
            {
                ExamId = command.ExamId,
                EnrollmentId = command.EnrollmentId,
                TheoryObtainedMarks = command.TheoryObtainedMarks,
                PracticalObtainedMarks = command.PracticalObtainedMarks,
                InternalMarks = command.InternalMarks,
                TheoryGraceMarks = command.TheoryGraceMarks,
                PracticalGraceMarks = command.PracticalGraceMarks,
                TheoryAbsent = command.TheoryAbsent,
                PracticalAbsent = command.PracticalAbsent,
                TotalMarks = totals.TotalMarks,
                IsAbsent = totals.IsAbsent,
                Remarks = command.Remarks,
                Exam = exam,
                Enrollment = enrollment
            };

            await _unitOfWork.ExamTerms.AddMarkAsync(mark, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var markDto = ExamMapper.ToStudentExamMarkDto(mark);
            var successResponse = CommonResponse<StudentExamMarkDto>.Success(markDto, "Student exam mark recorded successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<StudentExamMarkDto>> GetStudentExamMarkByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var mark = await _unitOfWork.ExamTerms.GetMarkByIdAsync(id, cancellationToken);
            if (mark == null)
            {
                var notFoundResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.NotFound, "Student exam mark with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var markDto = ExamMapper.ToStudentExamMarkDto(mark);
            var successResponse = CommonResponse<StudentExamMarkDto>.Success(markDto);
            return successResponse;
        }

        public async Task<CommonResponse<List<StudentExamMarkDto>>> GetStudentExamMarksAsync(Guid? examId, Guid? enrollmentId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            var marks = await _unitOfWork.ExamTerms.GetMarksAsync(examId, enrollmentId, classSectionId, cancellationToken);

            var markDtos = new List<StudentExamMarkDto>();
            foreach (var mark in marks)
            {
                var markDto = ExamMapper.ToStudentExamMarkDto(mark);
                markDtos.Add(markDto);
            }

            var successResponse = CommonResponse<List<StudentExamMarkDto>>.Success(markDtos);
            return successResponse;
        }

        // Admin, student-wise marks entry -- every exam within one term the enrollment is
        // eligible for, across every subject, each carrying its existing mark (or null). Reuses
        // GetExamsByTermAsync (already scoped to the enrollment's own class) and resolves
        // eligibility per exam exactly like GenerateExamResultsAsync/the roster does, so a subject
        // this student doesn't actually take (an elective they didn't pick, or a section-scoped
        // subject for a different section) never appears.
        public async Task<CommonResponse<List<StudentExamMarkByStudentItemDto>>> GetStudentExamMarksByStudentAsync(Guid enrollmentId, Guid examTermId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var enrollmentNotFoundResponse = CommonResponse<List<StudentExamMarkByStudentItemDto>>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return enrollmentNotFoundResponse;
            }

            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(examTermId, cancellationToken);
            if (examTerm == null)
            {
                var termNotFoundResponse = CommonResponse<List<StudentExamMarkByStudentItemDto>>.Fail(ResponseCodes.NotFound, "Exam term with id '" + examTermId + "' was not found.");
                return termNotFoundResponse;
            }

            var academicClassId = enrollment.ClassSection != null ? enrollment.ClassSection.AcademicClassId : Guid.Empty;
            var exams = await _unitOfWork.ExamTerms.GetExamsByTermAsync(examTermId, academicClassId, cancellationToken);

            var marks = await _unitOfWork.ExamTerms.GetMarksAsync(null, enrollmentId, null, cancellationToken);
            var marksByExamId = new Dictionary<Guid, StudentExamMark>();
            foreach (var mark in marks)
            {
                marksByExamId[mark.ExamId] = mark;
            }

            var items = new List<StudentExamMarkByStudentItemDto>();
            foreach (var exam in exams)
            {
                var eligibleEnrollments = await EligibleEnrollmentResolver.ResolveAsync(_unitOfWork, exam.ClassSubject, examTerm.AcademicYearId, null, cancellationToken);
                var isEligible = false;
                foreach (var eligibleEnrollment in eligibleEnrollments)
                {
                    if (eligibleEnrollment.Id == enrollmentId)
                    {
                        isEligible = true;
                        break;
                    }
                }

                if (!isEligible)
                {
                    continue;
                }

                var classSubject = exam.ClassSubject;
                marksByExamId.TryGetValue(exam.Id, out var existingMark);

                var item = new StudentExamMarkByStudentItemDto
                {
                    ExamId = exam.Id,
                    ClassSubjectId = exam.ClassSubjectId,
                    SubjectCode = classSubject != null ? classSubject.SubjectCode : null,
                    ExamDate = exam.ExamDate,
                    MarksLocked = exam.MarksLocked,
                    FullMarks = classSubject != null ? classSubject.FullMarks : null,
                    PassMarks = classSubject != null ? classSubject.PassMarks : null,
                    HasTheory = classSubject == null || classSubject.HasTheory,
                    HasPractical = classSubject != null && classSubject.HasPractical,
                    TheoryMarks = classSubject != null ? classSubject.TheoryMarks : null,
                    PracticalMarks = classSubject != null ? classSubject.PracticalMarks : null,
                    TheoryPassMarks = classSubject != null ? classSubject.TheoryPassMarks : null,
                    PracticalPassMarks = classSubject != null ? classSubject.PracticalPassMarks : null,
                    Mark = existingMark != null ? ExamMapper.ToStudentExamMarkDto(existingMark) : null
                };

                items.Add(item);
            }

            items.Sort(CompareByStudentItemsBySubjectCode);

            var successResponse = CommonResponse<List<StudentExamMarkByStudentItemDto>>.Success(items);
            return successResponse;
        }

        private static int CompareByStudentItemsBySubjectCode(StudentExamMarkByStudentItemDto first, StudentExamMarkByStudentItemDto second)
        {
            return string.Compare(first.SubjectCode, second.SubjectCode, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<CommonResponse<StudentExamMarkDto>> UpdateStudentExamMarkAsync(Guid id, UpdateStudentExamMarkCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateMarkValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var mark = await _unitOfWork.ExamTerms.GetMarkByIdAsync(id, cancellationToken);
            if (mark == null)
            {
                var notFoundResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.NotFound, "Student exam mark with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (mark.Exam.MarksLocked)
            {
                var lockedResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.ValidationError, "Marks entry is locked for this exam.");
                return lockedResponse;
            }

            var subjectValidationError = ValidateMarkAgainstSubject(mark.Exam.ClassSubject, command.TheoryObtainedMarks, command.PracticalObtainedMarks, command.TheoryGraceMarks, command.PracticalGraceMarks, command.TheoryAbsent, command.PracticalAbsent);
            if (subjectValidationError != null)
            {
                var subjectValidationResponse = CommonResponse<StudentExamMarkDto>.Fail(ResponseCodes.ValidationError, subjectValidationError);
                return subjectValidationResponse;
            }

            var totals = ComputeMarkTotals(mark.Exam.ClassSubject, command.TheoryObtainedMarks, command.PracticalObtainedMarks, command.InternalMarks, command.TheoryGraceMarks, command.PracticalGraceMarks, command.TheoryAbsent, command.PracticalAbsent);

            mark.TheoryObtainedMarks = command.TheoryObtainedMarks;
            mark.PracticalObtainedMarks = command.PracticalObtainedMarks;
            mark.InternalMarks = command.InternalMarks;
            mark.TheoryGraceMarks = command.TheoryGraceMarks;
            mark.PracticalGraceMarks = command.PracticalGraceMarks;
            mark.TheoryAbsent = command.TheoryAbsent;
            mark.PracticalAbsent = command.PracticalAbsent;
            mark.TotalMarks = totals.TotalMarks;
            mark.IsAbsent = totals.IsAbsent;
            mark.Remarks = command.Remarks;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var markDto = ExamMapper.ToStudentExamMarkDto(mark);
            var successResponse = CommonResponse<StudentExamMarkDto>.Success(markDto, "Student exam mark updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteStudentExamMarkAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var mark = await _unitOfWork.ExamTerms.GetMarkByIdAsync(id, cancellationToken);
            if (mark == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Student exam mark with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (mark.Exam.MarksLocked)
            {
                var lockedResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, "Marks entry is locked for this exam.");
                return lockedResponse;
            }

            _unitOfWork.ExamTerms.RemoveMark(mark);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Student exam mark deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<BulkUpsertStudentExamMarksResultDto>> BulkUpsertStudentExamMarksAsync(BulkUpsertStudentExamMarksCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _bulkUpsertMarksValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<BulkUpsertStudentExamMarksResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(command.ExamId, cancellationToken);
            if (exam == null)
            {
                var notFoundResponse = CommonResponse<BulkUpsertStudentExamMarksResultDto>.Fail(ResponseCodes.NotFound, "Exam with id '" + command.ExamId + "' was not found.");
                return notFoundResponse;
            }

            if (exam.MarksLocked)
            {
                var lockedResponse = CommonResponse<BulkUpsertStudentExamMarksResultDto>.Fail(ResponseCodes.ValidationError, "Marks entry is locked for this exam.");
                return lockedResponse;
            }

            var eligibleEnrollments = await EligibleEnrollmentResolver.ResolveAsync(_unitOfWork, exam.ClassSubject, exam.ExamTerm.AcademicYearId, null, cancellationToken);
            var eligibleEnrollmentIds = new HashSet<Guid>();
            foreach (var eligibleEnrollment in eligibleEnrollments)
            {
                eligibleEnrollmentIds.Add(eligibleEnrollment.Id);
            }

            var resultDto = new BulkUpsertStudentExamMarksResultDto { ExamId = command.ExamId };

            foreach (var line in command.Marks)
            {
                if (!eligibleEnrollmentIds.Contains(line.EnrollmentId))
                {
                    resultDto.Skipped.Add(new StudentExamMarkSkipDto { EnrollmentId = line.EnrollmentId, Reason = "This enrollment is not eligible for this exam's subject." });
                    continue;
                }

                var subjectValidationError = ValidateMarkAgainstSubject(exam.ClassSubject, line.TheoryObtainedMarks, line.PracticalObtainedMarks, line.TheoryGraceMarks, line.PracticalGraceMarks, line.TheoryAbsent, line.PracticalAbsent);
                if (subjectValidationError != null)
                {
                    resultDto.Skipped.Add(new StudentExamMarkSkipDto { EnrollmentId = line.EnrollmentId, Reason = subjectValidationError });
                    continue;
                }

                var totals = ComputeMarkTotals(exam.ClassSubject, line.TheoryObtainedMarks, line.PracticalObtainedMarks, line.InternalMarks, line.TheoryGraceMarks, line.PracticalGraceMarks, line.TheoryAbsent, line.PracticalAbsent);

                var existingMark = await _unitOfWork.ExamTerms.GetMarkByExamAndEnrollmentAsync(command.ExamId, line.EnrollmentId, cancellationToken);
                if (existingMark != null)
                {
                    existingMark.TheoryObtainedMarks = line.TheoryObtainedMarks;
                    existingMark.PracticalObtainedMarks = line.PracticalObtainedMarks;
                    existingMark.InternalMarks = line.InternalMarks;
                    existingMark.TheoryGraceMarks = line.TheoryGraceMarks;
                    existingMark.PracticalGraceMarks = line.PracticalGraceMarks;
                    existingMark.TheoryAbsent = line.TheoryAbsent;
                    existingMark.PracticalAbsent = line.PracticalAbsent;
                    existingMark.TotalMarks = totals.TotalMarks;
                    existingMark.IsAbsent = totals.IsAbsent;
                    existingMark.Remarks = line.Remarks;
                }
                else
                {
                    var newMark = new StudentExamMark
                    {
                        ExamId = command.ExamId,
                        EnrollmentId = line.EnrollmentId,
                        TheoryObtainedMarks = line.TheoryObtainedMarks,
                        PracticalObtainedMarks = line.PracticalObtainedMarks,
                        InternalMarks = line.InternalMarks,
                        TheoryGraceMarks = line.TheoryGraceMarks,
                        PracticalGraceMarks = line.PracticalGraceMarks,
                        TheoryAbsent = line.TheoryAbsent,
                        PracticalAbsent = line.PracticalAbsent,
                        TotalMarks = totals.TotalMarks,
                        IsAbsent = totals.IsAbsent,
                        Remarks = line.Remarks
                    };

                    await _unitOfWork.ExamTerms.AddMarkAsync(newMark, cancellationToken);
                }

                resultDto.UpsertedCount++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<BulkUpsertStudentExamMarksResultDto>.Success(resultDto, "Bulk marks upserted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<ExamMarkRosterItemDto>>> GetStudentExamMarkRosterAsync(Guid examId, string search, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            var exam = await _unitOfWork.ExamTerms.GetExamByIdAsync(examId, cancellationToken);
            if (exam == null)
            {
                var notFoundResponse = CommonResponse<List<ExamMarkRosterItemDto>>.Fail(ResponseCodes.NotFound, "Exam with id '" + examId + "' was not found.");
                return notFoundResponse;
            }

            var enrollments = await EligibleEnrollmentResolver.ResolveAsync(_unitOfWork, exam.ClassSubject, exam.ExamTerm.AcademicYearId, classSectionId, cancellationToken);
            var marks = await _unitOfWork.ExamTerms.GetMarksAsync(examId, null, classSectionId, cancellationToken);

            var marksByEnrollmentId = new Dictionary<Guid, StudentExamMark>();
            foreach (var mark in marks)
            {
                marksByEnrollmentId[mark.EnrollmentId] = mark;
            }

            var trimmedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var roster = new List<ExamMarkRosterItemDto>();
            foreach (var enrollment in enrollments)
            {
                var studentName = enrollment.Student != null ? (enrollment.Student.FirstName + " " + enrollment.Student.LastName) : null;
                var admissionNo = enrollment.Student != null ? enrollment.Student.AdmissionNo : null;

                if (trimmedSearch != null)
                {
                    var matchesName = studentName != null && studentName.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase);
                    var matchesAdmissionNo = admissionNo != null && admissionNo.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase);
                    if (!matchesName && !matchesAdmissionNo)
                    {
                        continue;
                    }
                }

                marksByEnrollmentId.TryGetValue(enrollment.Id, out var existingMark);

                var rosterItem = new ExamMarkRosterItemDto
                {
                    EnrollmentId = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    AdmissionNo = admissionNo,
                    StudentName = studentName,
                    RollNumber = enrollment.RollNumber,
                    Mark = existingMark != null ? ExamMapper.ToStudentExamMarkDto(existingMark) : null
                };

                roster.Add(rosterItem);
            }

            roster.Sort(CompareRosterByRollNumber);

            var successResponse = CommonResponse<List<ExamMarkRosterItemDto>>.Success(roster);
            return successResponse;
        }

        private static int CompareRosterByRollNumber(ExamMarkRosterItemDto first, ExamMarkRosterItemDto second)
        {
            return string.Compare(first.RollNumber, second.RollNumber, StringComparison.OrdinalIgnoreCase);
        }

        // --- Result Processing ---

        public async Task<CommonResponse<GenerateExamResultsResultDto>> GenerateExamResultsAsync(GenerateExamResultsCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _generateResultsValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<GenerateExamResultsResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(command.ExamTermId, cancellationToken);
            if (examTerm == null)
            {
                var notFoundResponse = CommonResponse<GenerateExamResultsResultDto>.Fail(ResponseCodes.NotFound, "Exam term with id '" + command.ExamTermId + "' was not found.");
                return notFoundResponse;
            }

            // Every Exam in the term counts (no more "final exam" filter -- there's exactly one
            // sitting per subject per term now), optionally narrowed to one grade.
            var exams = await _unitOfWork.ExamTerms.GetExamsByTermAsync(command.ExamTermId, command.AcademicClassId, cancellationToken);

            var resultDto = new GenerateExamResultsResultDto { ExamTermId = command.ExamTermId };

            if (exams.Count == 0)
            {
                var noExamsResponse = CommonResponse<GenerateExamResultsResultDto>.Fail(ResponseCodes.ValidationError, "No exams were found for the given scope.");
                return noExamsResponse;
            }

            var allExamIds = new List<Guid>();
            foreach (var exam in exams)
            {
                allExamIds.Add(exam.Id);
            }

            var allMarks = await _unitOfWork.ExamTerms.GetMarksByExamIdsAsync(allExamIds, cancellationToken);
            var marksByKey = new Dictionary<string, StudentExamMark>();
            foreach (var mark in allMarks)
            {
                var key = mark.ExamId + "|" + mark.EnrollmentId;
                marksByKey[key] = mark;
            }

            // An Exam no longer pins a section -- resolve, per exam, which enrollments actually
            // take that subject (whole grade for mandatory, electors only for optional), then
            // invert into a per-enrollment bag of every exam contributing to that student's
            // result (a student contributes to as many exams as subjects they take).
            var enrollmentsById = new Dictionary<Guid, Enrollment>();
            var examsByEnrollmentId = new Dictionary<Guid, List<Exam>>();

            foreach (var exam in exams)
            {
                var eligibleEnrollments = await EligibleEnrollmentResolver.ResolveAsync(_unitOfWork, exam.ClassSubject, examTerm.AcademicYearId, command.ClassSectionId, cancellationToken);
                foreach (var enrollment in eligibleEnrollments)
                {
                    enrollmentsById[enrollment.Id] = enrollment;

                    if (!examsByEnrollmentId.TryGetValue(enrollment.Id, out var examList))
                    {
                        examList = new List<Exam>();
                        examsByEnrollmentId[enrollment.Id] = examList;
                    }

                    examList.Add(exam);
                }
            }

            // Ranking is still per the enrollment's own section (Enrollment.ClassSectionId),
            // independent of how many exams contributed to the result.
            var resultsBySection = new Dictionary<Guid, List<StudentResult>>();

            foreach (var entry in examsByEnrollmentId)
            {
                var enrollmentId = entry.Key;
                var applicableExams = entry.Value;
                var enrollment = enrollmentsById[enrollmentId];
                var studentName = enrollment.Student != null ? (enrollment.Student.FirstName + " " + enrollment.Student.LastName) : null;

                var allLocked = true;
                string unlockedSubjectCode = null;
                foreach (var exam in applicableExams)
                {
                    if (!exam.MarksLocked)
                    {
                        allLocked = false;
                        unlockedSubjectCode = exam.ClassSubject != null ? exam.ClassSubject.SubjectCode : null;
                        break;
                    }
                }

                if (!allLocked)
                {
                    resultDto.Skipped.Add(new ExamResultGenerationSkipDto { EnrollmentId = enrollmentId, StudentName = studentName, Reason = "The exam for subject '" + unlockedSubjectCode + "' is not locked yet." });
                    continue;
                }

                var existingResult = await _unitOfWork.ExamTerms.GetResultByEnrollmentAndTermIgnoringFiltersAsync(enrollmentId, command.ExamTermId, cancellationToken);
                if (existingResult != null && existingResult.ResultStatus == ResultStatus.Withheld && !existingResult.IsDeleted)
                {
                    resultDto.Skipped.Add(new ExamResultGenerationSkipDto { EnrollmentId = enrollmentId, StudentName = studentName, Reason = "Result is withheld; lift the hold before regenerating." });
                    continue;
                }

                decimal totalFullMarks = 0;
                decimal totalObtainedMarks = 0;
                decimal gradePointWeightedSum = 0;
                decimal gradePointWeightTotal = 0;
                var failedSubjectCount = 0;
                var skipEnrollment = false;
                string skipReason = null;

                foreach (var exam in applicableExams)
                {
                    var classSubject = exam.ClassSubject;
                    if (classSubject == null || !classSubject.FullMarks.HasValue || !classSubject.PassMarks.HasValue)
                    {
                        skipEnrollment = true;
                        skipReason = "Full Marks/Pass Marks are not configured for subject '" + (classSubject != null ? classSubject.SubjectCode : exam.ClassSubjectId.ToString()) + "'.";
                        break;
                    }

                    var key = exam.Id + "|" + enrollmentId;
                    if (!marksByKey.TryGetValue(key, out var mark))
                    {
                        skipEnrollment = true;
                        skipReason = "Marks are missing for subject '" + classSubject.SubjectCode + "'.";
                        break;
                    }

                    var subjectFullMarks = (decimal)classSubject.FullMarks.Value;

                    totalFullMarks += subjectFullMarks;
                    totalObtainedMarks += mark.TotalMarks;

                    var subjectPassed = IsSubjectPassed(classSubject, mark);
                    if (!subjectPassed)
                    {
                        failedSubjectCount++;
                    }

                    var subjectPercentage = subjectFullMarks > 0 ? Math.Round(mark.TotalMarks / subjectFullMarks * 100, 2) : 0m;
                    var gradeScale = await _unitOfWork.GradeScales.FindByPercentageAsync(subjectPercentage, cancellationToken);

                    mark.Grade = gradeScale != null ? gradeScale.Grade : null;
                    mark.GradePoint = gradeScale != null ? gradeScale.GradePoint : (decimal?)null;

                    var creditWeight = classSubject.CreditHours.HasValue ? classSubject.CreditHours.Value : 1m;
                    if (mark.GradePoint.HasValue)
                    {
                        gradePointWeightedSum += mark.GradePoint.Value * creditWeight;
                        gradePointWeightTotal += creditWeight;
                    }
                }

                if (skipEnrollment)
                {
                    resultDto.Skipped.Add(new ExamResultGenerationSkipDto { EnrollmentId = enrollmentId, StudentName = studentName, Reason = skipReason });
                    continue;
                }

                var percentage = totalFullMarks > 0 ? Math.Round(totalObtainedMarks / totalFullMarks * 100, 2) : 0m;
                var gpa = gradePointWeightTotal > 0 ? Math.Round(gradePointWeightedSum / gradePointWeightTotal, 2) : 0m;

                ResultStatus resultStatus;
                if (failedSubjectCount == 0)
                {
                    resultStatus = ResultStatus.Pass;
                }
                else if (failedSubjectCount <= ExamResultRules.CompartmentMaxFailedSubjects)
                {
                    resultStatus = ResultStatus.Compartment;
                }
                else
                {
                    resultStatus = ResultStatus.Fail;
                }

                StudentResult studentResult;
                if (existingResult != null)
                {
                    studentResult = existingResult;
                    studentResult.IsDeleted = false;
                    studentResult.DeletedBy = null;
                    studentResult.DeletedTs = null;
                }
                else
                {
                    studentResult = new StudentResult
                    {
                        EnrollmentId = enrollmentId,
                        ExamTermId = command.ExamTermId
                    };
                    await _unitOfWork.ExamTerms.AddResultAsync(studentResult, cancellationToken);
                }

                studentResult.TotalMarks = totalFullMarks;
                studentResult.ObtainedMarks = totalObtainedMarks;
                studentResult.Percentage = percentage;
                studentResult.GPA = gpa;
                studentResult.ResultStatus = resultStatus;

                if (!resultsBySection.TryGetValue(enrollment.ClassSectionId, out var sectionResults))
                {
                    sectionResults = new List<StudentResult>();
                    resultsBySection[enrollment.ClassSectionId] = sectionResults;
                }

                sectionResults.Add(studentResult);
                resultDto.GeneratedCount++;
            }

            foreach (var sectionResults in resultsBySection.Values)
            {
                for (var i = 0; i < sectionResults.Count; i++)
                {
                    var bestIndex = i;
                    for (var j = i + 1; j < sectionResults.Count; j++)
                    {
                        if (IsHigherRanked(sectionResults[j], sectionResults[bestIndex]))
                        {
                            bestIndex = j;
                        }
                    }

                    if (bestIndex != i)
                    {
                        var temp = sectionResults[i];
                        sectionResults[i] = sectionResults[bestIndex];
                        sectionResults[bestIndex] = temp;
                    }
                }

                for (var index = 0; index < sectionResults.Count; index++)
                {
                    sectionResults[index].Rank = index + 1;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<GenerateExamResultsResultDto>.Success(resultDto, "Exam results generated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> PublishExamResultsAsync(Guid examTermId, CancellationToken cancellationToken = default)
        {
            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(examTermId, cancellationToken);
            if (examTerm == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Exam term with id '" + examTermId + "' was not found.");
                return notFoundResponse;
            }

            var results = await _unitOfWork.ExamTerms.GetResultsByTermAsync(examTermId, cancellationToken);
            var publishedTs = DateTime.UtcNow;

            foreach (var result in results)
            {
                if (result.ResultStatus == ResultStatus.Withheld)
                {
                    continue;
                }

                result.PublishedDate = publishedTs;
            }

            examTerm.PublishResult = true;

            var termExams = await _unitOfWork.ExamTerms.GetExamsByTermAsync(examTermId, null, cancellationToken);
            var examIds = new List<Guid>();
            foreach (var exam in termExams)
            {
                examIds.Add(exam.Id);
            }

            var marks = await _unitOfWork.ExamTerms.GetMarksByExamIdsAsync(examIds, cancellationToken);
            foreach (var mark in marks)
            {
                mark.IsPublished = true;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Exam results published for this term.");
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<StudentResultDto>>> GetStudentResultsAsync(Guid? examTermId, Guid? classSectionId, Guid? enrollmentId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedResults = await _unitOfWork.ExamTerms.GetResultsPagedByFilterAsync(examTermId, classSectionId, enrollmentId, page, pageSize, cancellationToken);

            var resultDtos = new List<StudentResultDto>();
            foreach (var result in pagedResults.Items)
            {
                var resultDto = ExamMapper.ToStudentResultDto(result);
                resultDtos.Add(resultDto);
            }

            var paginatedResponse = new PaginatedResponse<StudentResultDto>
            {
                Items = resultDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedResults.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<StudentResultDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<StudentResultDetailDto>> GetStudentResultByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _unitOfWork.ExamTerms.GetResultByIdAsync(id, cancellationToken);
            if (result == null)
            {
                var notFoundResponse = CommonResponse<StudentResultDetailDto>.Fail(ResponseCodes.NotFound, "Student result with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var detailDto = ExamMapper.ToStudentResultDetailDto(result);

            // Which subjects contributed is answered directly by "does this student have a mark
            // for that exam" -- the same source of truth generation itself used -- rather than
            // re-deriving eligibility a second time.
            var studentMarks = await _unitOfWork.ExamTerms.GetMarksAsync(null, result.EnrollmentId, null, cancellationToken);

            foreach (var mark in studentMarks)
            {
                if (mark.Exam == null || mark.Exam.ExamTermId != result.ExamTermId)
                {
                    continue;
                }

                var classSubject = mark.Exam.ClassSubject;
                var subjectDto = new ExamResultSubjectDto
                {
                    ClassSubjectId = mark.Exam.ClassSubjectId,
                    SubjectCode = classSubject != null ? classSubject.SubjectCode : null,
                    FullMarks = classSubject != null ? classSubject.FullMarks : null,
                    PassMarks = classSubject != null ? classSubject.PassMarks : null,
                    ObtainedMarks = mark.TotalMarks,
                    Grade = mark.Grade,
                    GradePoint = mark.GradePoint,
                    IsAbsent = mark.IsAbsent,
                    Passed = classSubject != null && IsSubjectPassed(classSubject, mark)
                };

                detailDto.Subjects.Add(subjectDto);
            }

            var successResponse = CommonResponse<StudentResultDetailDto>.Success(detailDto);
            return successResponse;
        }

        public async Task<CommonResponse<StudentResultDto>> WithholdExamResultAsync(Guid id, WithholdExamResultCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _withholdResultValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var result = await _unitOfWork.ExamTerms.GetResultByIdAsync(id, cancellationToken);
            if (result == null)
            {
                var notFoundResponse = CommonResponse<StudentResultDto>.Fail(ResponseCodes.NotFound, "Student result with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            result.ResultStatus = ResultStatus.Withheld;
            result.Remarks = command.Remarks.Trim();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = ExamMapper.ToStudentResultDto(result);
            var successResponse = CommonResponse<StudentResultDto>.Success(resultDto, "Exam result withheld.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> LiftExamResultWithholdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _unitOfWork.ExamTerms.GetResultByIdAsync(id, cancellationToken);
            if (result == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Student result with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (result.ResultStatus != ResultStatus.Withheld)
            {
                var notWithheldResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, "This result is not withheld.");
                return notWithheldResponse;
            }

            _unitOfWork.ExamTerms.RemoveResult(result);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Withhold lifted. Regenerate results for this term/section to recompute this student's record.");
            return successResponse;
        }

        private static bool IsHigherRanked(StudentResult candidate, StudentResult current)
        {
            if (candidate.Percentage != current.Percentage)
            {
                return candidate.Percentage > current.Percentage;
            }

            return candidate.ObtainedMarks > current.ObtainedMarks;
        }

        // Overall pass/fail also reads ClassSubject.PassMarks now (guaranteed configured before
        // an Exam can even be created for the subject, see ValidateSubjectMarksConfigured) --
        // there is no longer a separate schedule-level PassMarks to fall back to.
        private static bool IsSubjectPassed(ClassSubject classSubject, StudentExamMark mark)
        {
            if (mark.IsAbsent)
            {
                return false;
            }

            if (classSubject.HasTheory && classSubject.TheoryPassMarks.HasValue)
            {
                var theoryTotal = (mark.TheoryObtainedMarks ?? 0) + mark.TheoryGraceMarks;
                if (theoryTotal < classSubject.TheoryPassMarks.Value)
                {
                    return false;
                }
            }

            if (classSubject.HasPractical && classSubject.PracticalPassMarks.HasValue)
            {
                var practicalTotal = (mark.PracticalObtainedMarks ?? 0) + mark.PracticalGraceMarks;
                if (practicalTotal < classSubject.PracticalPassMarks.Value)
                {
                    return false;
                }
            }

            if (classSubject.PassMarks.HasValue && mark.TotalMarks < classSubject.PassMarks.Value)
            {
                return false;
            }

            return true;
        }

        // Validates a submitted mark against its ClassSubject's HasTheory/HasPractical
        // configuration and each component's own full-marks cap -- see the design doc's section
        // 1.3 "Individual Component Passing" rule and the assessment-configuration guide.
        private static string ValidateMarkAgainstSubject(ClassSubject classSubject, decimal? theoryObtained, decimal? practicalObtained, decimal theoryGrace, decimal practicalGrace, bool theoryAbsent, bool practicalAbsent)
        {
            if (classSubject == null)
            {
                return null;
            }

            if (!classSubject.HasTheory && (theoryObtained.HasValue || theoryGrace != 0 || theoryAbsent))
            {
                return "This subject has no theory component; TheoryObtainedMarks/TheoryGraceMarks/TheoryAbsent cannot be set.";
            }

            if (!classSubject.HasPractical && (practicalObtained.HasValue || practicalGrace != 0 || practicalAbsent))
            {
                return "This subject has no practical component; PracticalObtainedMarks/PracticalGraceMarks/PracticalAbsent cannot be set.";
            }

            if (classSubject.HasTheory && !theoryAbsent && !theoryObtained.HasValue)
            {
                return "TheoryObtainedMarks is required unless TheoryAbsent is true.";
            }

            if (classSubject.HasPractical && !practicalAbsent && !practicalObtained.HasValue)
            {
                return "PracticalObtainedMarks is required unless PracticalAbsent is true.";
            }

            if (classSubject.HasTheory && theoryObtained.HasValue && classSubject.TheoryMarks.HasValue && theoryObtained.Value > classSubject.TheoryMarks.Value)
            {
                return "TheoryObtainedMarks cannot exceed the subject's TheoryMarks (" + classSubject.TheoryMarks.Value + ").";
            }

            if (classSubject.HasPractical && practicalObtained.HasValue && classSubject.PracticalMarks.HasValue && practicalObtained.Value > classSubject.PracticalMarks.Value)
            {
                return "PracticalObtainedMarks cannot exceed the subject's PracticalMarks (" + classSubject.PracticalMarks.Value + ").";
            }

            return null;
        }

        // TotalMarks = Theory + Practical + Internal + Grace (design doc section 3.1) -- an
        // absent component contributes zero, grace marks included, regardless of the component
        // not applying to this subject (ValidateMarkAgainstSubject already refused that case).
        // IsAbsent is true only when every component the subject actually has was marked absent.
        private static (decimal TotalMarks, bool IsAbsent) ComputeMarkTotals(ClassSubject classSubject, decimal? theoryObtained, decimal? practicalObtained, decimal? internalMarks, decimal theoryGrace, decimal practicalGrace, bool theoryAbsent, bool practicalAbsent)
        {
            var hasTheory = classSubject == null || classSubject.HasTheory;
            var hasPractical = classSubject != null && classSubject.HasPractical;

            var theoryComponent = hasTheory && !theoryAbsent ? (theoryObtained ?? 0) + theoryGrace : 0m;
            var practicalComponent = hasPractical && !practicalAbsent ? (practicalObtained ?? 0) + practicalGrace : 0m;
            var internalComponent = internalMarks ?? 0m;
            var totalMarks = theoryComponent + practicalComponent + internalComponent;

            bool isAbsent;
            if (hasTheory && hasPractical)
            {
                isAbsent = theoryAbsent && practicalAbsent;
            }
            else if (hasTheory)
            {
                isAbsent = theoryAbsent;
            }
            else
            {
                isAbsent = practicalAbsent;
            }

            return (totalMarks, isAbsent);
        }

        private async Task<CalendarEvent> BuildExamCalendarEventAsync(string examTermName, string subjectCode, DateTime examDate, CancellationToken cancellationToken)
        {
            var (bsYear, bsMonth, bsDay) = await _conversionService.ConvertAdToBsAsync(examDate, cancellationToken);

            var calendarEvent = new CalendarEvent
            {
                Id = Guid.NewGuid(),
                Title = BuildExamEventTitle(examTermName, subjectCode),
                EventType = CalendarEventType.Exam,
                AdDate = examDate,
                BsYear = bsYear,
                BsMonth = bsMonth,
                BsDay = bsDay,
                IsActive = true
            };

            return calendarEvent;
        }

        private static string BuildExamEventTitle(string examTermName, string subjectCode)
        {
            return examTermName != null ? examTermName + ": " + subjectCode : subjectCode;
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
