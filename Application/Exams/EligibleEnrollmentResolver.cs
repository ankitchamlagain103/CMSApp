using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Exams
{
    // Resolves which enrollments a ClassSubject actually applies to -- the load-bearing piece of
    // the section-less Exam redesign (Docs/Exam_Module_Design_Revised.md). An Exam only pins a
    // subject (and therefore a grade, via ClassSubject.AcademicClassId), never a section, so
    // "which students take this exam" has to be derived: the whole grade for a mandatory,
    // class-wide subject; just the students who elected it for an optional one; just one section
    // for a section-scoped subject (always optional -- IsMandatory + ClassSectionId together is
    // already rejected at subject-assignment time). Used by ExamService for marks roster and
    // result generation.
    public static class EligibleEnrollmentResolver
    {
        public static async Task<IReadOnlyList<Enrollment>> ResolveAsync(IUnitOfWork unitOfWork, ClassSubject classSubject, Guid academicYearId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            // A caller-requested section (e.g. "generate results for section X") and a subject
            // that's itself scoped to a different section have no overlap at all -- return empty
            // rather than silently ignoring one or the other.
            if (classSectionId.HasValue && classSubject.ClassSectionId.HasValue && classSectionId.Value != classSubject.ClassSectionId.Value)
            {
                return new List<Enrollment>();
            }

            var effectiveSectionId = classSectionId ?? classSubject.ClassSectionId;

            var enrollments = await unitOfWork.Enrollments.GetEnrolledByYearAsync(academicYearId, classSubject.AcademicClassId, effectiveSectionId, cancellationToken);

            if (classSubject.IsMandatory)
            {
                return enrollments;
            }

            var enrollmentIds = new List<Guid>();
            foreach (var enrollment in enrollments)
            {
                enrollmentIds.Add(enrollment.Id);
            }

            if (enrollmentIds.Count == 0)
            {
                return new List<Enrollment>();
            }

            var electiveSubjects = await unitOfWork.Enrollments.GetElectiveSubjectsByEnrollmentIdsAsync(enrollmentIds, cancellationToken);

            var electedEnrollmentIds = new List<Guid>();
            foreach (var electiveSubject in electiveSubjects)
            {
                if (electiveSubject.ClassSubjectId == classSubject.Id)
                {
                    electedEnrollmentIds.Add(electiveSubject.EnrollmentId);
                }
            }

            var eligibleEnrollments = new List<Enrollment>();
            foreach (var enrollment in enrollments)
            {
                if (electedEnrollmentIds.Contains(enrollment.Id))
                {
                    eligibleEnrollments.Add(enrollment);
                }
            }

            return eligibleEnrollments;
        }
    }
}
