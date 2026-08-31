using Application.Common.Helpers;
using Application.Exams.Dtos;
using Domain.Entities;

namespace Application.Exams
{
    public static class ExamMapper
    {
        // labelsByCode (2026-08-05): merged Grade+Section+Subject Config label map; null keeps
        // every Label field at its raw code. See Docs/config_label_resolution_implementation_guide.md.
        public static StudentResultDto ToStudentResultDto(StudentResult result, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var gradeCode = result.Enrollment != null && result.Enrollment.ClassSection != null && result.Enrollment.ClassSection.AcademicClass != null ? result.Enrollment.ClassSection.AcademicClass.GradeCode : null;
            var sectionCode = result.Enrollment != null && result.Enrollment.ClassSection != null ? result.Enrollment.ClassSection.SectionCode : null;

            var studentResultDto = new StudentResultDto
            {
                Id = result.Id,
                EnrollmentId = result.EnrollmentId,
                StudentName = result.Enrollment != null && result.Enrollment.Student != null ? (result.Enrollment.Student.FirstName + " " + result.Enrollment.Student.LastName) : null,
                AdmissionNo = result.Enrollment != null && result.Enrollment.Student != null ? result.Enrollment.Student.AdmissionNo : null,
                GradeCode = gradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(labelsByCode, gradeCode),
                SectionCode = sectionCode,
                SectionLabel = ConfigLabelHelper.Resolve(labelsByCode, sectionCode),
                ExamTermId = result.ExamTermId,
                TotalMarks = result.TotalMarks,
                ObtainedMarks = result.ObtainedMarks,
                Percentage = result.Percentage,
                GPA = result.GPA,
                Rank = result.Rank,
                ResultStatus = result.ResultStatus,
                PublishedDate = result.PublishedDate,
                Remarks = result.Remarks
            };

            return studentResultDto;
        }

        public static StudentResultDetailDto ToStudentResultDetailDto(StudentResult result, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var gradeCode = result.Enrollment != null && result.Enrollment.ClassSection != null && result.Enrollment.ClassSection.AcademicClass != null ? result.Enrollment.ClassSection.AcademicClass.GradeCode : null;
            var sectionCode = result.Enrollment != null && result.Enrollment.ClassSection != null ? result.Enrollment.ClassSection.SectionCode : null;

            var studentResultDetailDto = new StudentResultDetailDto
            {
                Id = result.Id,
                EnrollmentId = result.EnrollmentId,
                StudentName = result.Enrollment != null && result.Enrollment.Student != null ? (result.Enrollment.Student.FirstName + " " + result.Enrollment.Student.LastName) : null,
                AdmissionNo = result.Enrollment != null && result.Enrollment.Student != null ? result.Enrollment.Student.AdmissionNo : null,
                GradeCode = gradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(labelsByCode, gradeCode),
                SectionCode = sectionCode,
                SectionLabel = ConfigLabelHelper.Resolve(labelsByCode, sectionCode),
                ExamTermId = result.ExamTermId,
                TotalMarks = result.TotalMarks,
                ObtainedMarks = result.ObtainedMarks,
                Percentage = result.Percentage,
                GPA = result.GPA,
                Rank = result.Rank,
                ResultStatus = result.ResultStatus,
                PublishedDate = result.PublishedDate,
                Remarks = result.Remarks
            };

            return studentResultDetailDto;
        }

        public static ExamTermDto ToExamTermDto(ExamTerm examTerm)
        {
            var examTermDto = new ExamTermDto
            {
                Id = examTerm.Id,
                AcademicYearId = examTerm.AcademicYearId,
                Code = examTerm.Code,
                Name = examTerm.Name,
                Sequence = examTerm.Sequence,
                StartDate = examTerm.StartDate,
                EndDate = examTerm.EndDate,
                PublishResult = examTerm.PublishResult,
                Status = examTerm.Status
            };

            return examTermDto;
        }

        public static ExamDto ToExamDto(Exam exam, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var classSubject = exam.ClassSubject;
            var subjectCode = classSubject != null ? classSubject.SubjectCode : null;
            var gradeCode = classSubject != null && classSubject.AcademicClass != null ? classSubject.AcademicClass.GradeCode : null;

            var examDto = new ExamDto
            {
                Id = exam.Id,
                ExamTermId = exam.ExamTermId,
                ClassSubjectId = exam.ClassSubjectId,
                SubjectCode = subjectCode,
                SubjectLabel = ConfigLabelHelper.Resolve(labelsByCode, subjectCode),
                GradeCode = gradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(labelsByCode, gradeCode),
                ExamDate = exam.ExamDate,
                TimePeriodId = exam.TimePeriodId,
                TimePeriodName = exam.TimePeriod != null ? exam.TimePeriod.Name : null,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                Remarks = exam.Remarks,
                MarksLocked = exam.MarksLocked,
                FullMarks = classSubject != null ? classSubject.FullMarks : null,
                PassMarks = classSubject != null ? classSubject.PassMarks : null,
                HasTheory = classSubject == null || classSubject.HasTheory,
                HasPractical = classSubject != null && classSubject.HasPractical,
                TheoryMarks = classSubject != null ? classSubject.TheoryMarks : null,
                PracticalMarks = classSubject != null ? classSubject.PracticalMarks : null,
                TheoryPassMarks = classSubject != null ? classSubject.TheoryPassMarks : null,
                PracticalPassMarks = classSubject != null ? classSubject.PracticalPassMarks : null
            };

            return examDto;
        }

        public static StudentExamMarkDto ToStudentExamMarkDto(StudentExamMark mark, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var subjectCode = mark.Exam != null && mark.Exam.ClassSubject != null ? mark.Exam.ClassSubject.SubjectCode : null;

            var studentExamMarkDto = new StudentExamMarkDto
            {
                Id = mark.Id,
                ExamId = mark.ExamId,
                SubjectCode = subjectCode,
                SubjectLabel = ConfigLabelHelper.Resolve(labelsByCode, subjectCode),
                EnrollmentId = mark.EnrollmentId,
                StudentName = mark.Enrollment != null && mark.Enrollment.Student != null ? (mark.Enrollment.Student.FirstName + " " + mark.Enrollment.Student.LastName) : null,
                AdmissionNo = mark.Enrollment != null && mark.Enrollment.Student != null ? mark.Enrollment.Student.AdmissionNo : null,
                TheoryObtainedMarks = mark.TheoryObtainedMarks,
                PracticalObtainedMarks = mark.PracticalObtainedMarks,
                InternalMarks = mark.InternalMarks,
                TheoryGraceMarks = mark.TheoryGraceMarks,
                PracticalGraceMarks = mark.PracticalGraceMarks,
                TheoryAbsent = mark.TheoryAbsent,
                PracticalAbsent = mark.PracticalAbsent,
                TotalMarks = mark.TotalMarks,
                Grade = mark.Grade,
                GradePoint = mark.GradePoint,
                Remarks = mark.Remarks,
                IsAbsent = mark.IsAbsent,
                IsPublished = mark.IsPublished
            };

            return studentExamMarkDto;
        }
    }
}
