using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Teachers
{
    // Shared by TeacherService (AssignClassSubjectAsync / AssignClassSubjectBulkAsync /
    // AssignClassSubjectBulkEntryAsync -- "assign this teacher") and AcademicClassService
    // (AssignTeachersBulkEntryAsync -- "who teaches this class", mapping several teachers onto
    // one class in a single screen). The exact same (teacher, classSubject, section, timePeriod)
    // validation applies regardless of which side of the relationship the caller started from, so
    // it lives here once instead of being duplicated per entry point.
    public static class TeacherAssignmentBuilder
    {
        // Validates one (teacher, classSubject, requestedClassSectionId) combination and builds
        // the TeacherAssignment to add, or returns an error code/message without touching the
        // database. Callers add the returned assignment and call SaveChangesAsync themselves --
        // a bulk caller needs to keep accumulating several before one save.
        public static async Task<(TeacherAssignment Assignment, string ErrorCode, string ErrorMessage)> BuildAsync(IUnitOfWork unitOfWork, Guid teacherId, ClassSubject classSubject, Guid? requestedClassSectionId, bool isClassTeacher, Guid? timePeriodId, CancellationToken cancellationToken)
        {
            Guid? effectiveClassSectionId = requestedClassSectionId;
            ClassSection classSection = null;
            if (classSubject.ClassSectionId.HasValue)
            {
                // The assignment's section is DERIVED from the subject whenever the subject is
                // itself section-scoped, rather than left for the caller to (possibly wrongly)
                // repeat: a section-scoped subject can only ever be taught in its own section, so
                // there is exactly one valid value and the caller doesn't need to supply it.
                if (requestedClassSectionId.HasValue && requestedClassSectionId.Value != classSubject.ClassSectionId.Value)
                {
                    return (null, ResponseCodes.ValidationError, "Subject '" + classSubject.SubjectCode + "' is only offered in a different section.");
                }

                effectiveClassSectionId = classSubject.ClassSectionId;
                classSection = classSubject.ClassSection;
            }
            else
            {
                // ClassSectionId is REQUIRED for a class-wide subject (2026-08-04) -- a teacher
                // cannot physically teach every section of a class at once, so "leave it null to
                // cover every section in one assignment" is no longer an allowed shape for a new
                // assignment. (Rows created before this date may still carry a null
                // ClassSectionId from that earlier, now-retired behavior -- see the read-side
                // Scope/ClassWide comments on TeacherAssignmentDto.)
                if (!requestedClassSectionId.HasValue)
                {
                    return (null, ResponseCodes.ValidationError, "ClassSectionId is required -- a teacher must be assigned to one specific section, not every section of the class at once.");
                }

                classSection = await unitOfWork.AcademicClasses.GetSectionByIdAsync(requestedClassSectionId.Value, cancellationToken);
                if (classSection == null)
                {
                    return (null, ResponseCodes.NotFound, "Class section with id '" + requestedClassSectionId.Value + "' was not found.");
                }

                if (classSection.AcademicClassId != classSubject.AcademicClassId)
                {
                    return (null, ResponseCodes.ValidationError, "That section belongs to a different class than the subject.");
                }
            }

            // effectiveClassSectionId is guaranteed non-null past this point -- either derived
            // from a section-scoped subject or required-and-validated above.
            var assignmentExists = await unitOfWork.Teachers.AssignmentExistsAsync(teacherId, classSubject.Id, effectiveClassSectionId, cancellationToken);
            if (assignmentExists)
            {
                return (null, ResponseCodes.Conflict, "This teacher is already assigned to that class subject for that section.");
            }

            // A section has at most one class teacher, regardless of which subject the
            // class-teacher assignment rides on.
            if (isClassTeacher)
            {
                var classTeacherExists = await unitOfWork.Teachers.ClassTeacherExistsForSectionAsync(effectiveClassSectionId.Value, cancellationToken);
                if (classTeacherExists)
                {
                    return (null, ResponseCodes.Conflict, "This section already has a class teacher. Remove that assignment first.");
                }
            }

            TimePeriod timePeriod = null;
            if (timePeriodId.HasValue)
            {
                timePeriod = await unitOfWork.TimePeriods.GetByIdAsync(timePeriodId.Value, cancellationToken);
                if (timePeriod == null)
                {
                    return (null, ResponseCodes.ValidationError, "Time period with id '" + timePeriodId.Value + "' was not found.");
                }

                if (timePeriod.Kind == PeriodKind.Break)
                {
                    return (null, ResponseCodes.ValidationError, "'" + timePeriod.Name + "' is a break, not a teaching period.");
                }

                var isMapped = await unitOfWork.TimePeriods.IsMappedToClassAsync(classSubject.AcademicClassId, timePeriodId.Value, cancellationToken);
                if (!isMapped)
                {
                    return (null, ResponseCodes.ValidationError, "'" + timePeriod.Name + "' is not mapped to this class -- map it first via the Time Periods screen.");
                }

                // 2026-08-04: a teacher can only be in one class/section during a given period --
                // checked against every one of the teacher's OTHER existing assignments,
                // regardless of which class/subject/section they're for. This only sees rows
                // already committed to the database; a bulk caller staging several rows in one
                // request must additionally guard against two rows in the SAME batch colliding
                // with each other (nothing is saved until the batch's own SaveChangesAsync).
                var hasTimePeriodConflict = await unitOfWork.Teachers.TeacherHasTimePeriodConflictAsync(teacherId, timePeriodId.Value, cancellationToken);
                if (hasTimePeriodConflict)
                {
                    return (null, ResponseCodes.Conflict, "This teacher is already assigned to another class/section during '" + timePeriod.Name + "'.");
                }
            }

            var assignment = new TeacherAssignment
            {
                TeacherId = teacherId,
                ClassSubjectId = classSubject.Id,
                ClassSectionId = effectiveClassSectionId,
                IsClassTeacher = isClassTeacher,
                TimePeriodId = timePeriodId,
                ClassSubject = classSubject,
                ClassSection = classSection,
                TimePeriod = timePeriod
            };

            return (assignment, null, null);
        }
    }
}
