using Application.Exams.Dtos;
using Domain.Entities;

namespace Application.Exams
{
    public static class ExamMapper
    {
        public static StudentResultDto ToStudentResultDto(StudentResult result)
        {
            var studentResultDto = new StudentResultDto
            {
                Id = result.Id,
                EnrollmentId = result.EnrollmentId,
                StudentName = result.Enrollment != null && result.Enrollment.Student != null ? (result.Enrollment.Student.FirstName + " " + result.Enrollment.Student.LastName) : null,
                AdmissionNo = result.Enrollment != null && result.Enrollment.Student != null ? result.Enrollment.Student.AdmissionNo : null,
                GradeCode = result.Enrollment != null && result.Enrollment.ClassSection != null && result.Enrollment.ClassSection.AcademicClass != null ? result.Enrollment.ClassSection.AcademicClass.GradeCode : null,
                SectionCode = result.Enrollment != null && result.Enrollment.ClassSection != null ? result.Enrollment.ClassSection.SectionCode : null,
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

        public static StudentResultDetailDto ToStudentResultDetailDto(StudentResult result)
        {
            var studentResultDetailDto = new StudentResultDetailDto
            {
                Id = result.Id,
                EnrollmentId = result.EnrollmentId,
                StudentName = result.Enrollment != null && result.Enrollment.Student != null ? (result.Enrollment.Student.FirstName + " " + result.Enrollment.Student.LastName) : null,
                AdmissionNo = result.Enrollment != null && result.Enrollment.Student != null ? result.Enrollment.Student.AdmissionNo : null,
                GradeCode = result.Enrollment != null && result.Enrollment.ClassSection != null && result.Enrollment.ClassSection.AcademicClass != null ? result.Enrollment.ClassSection.AcademicClass.GradeCode : null,
                SectionCode = result.Enrollment != null && result.Enrollment.ClassSection != null ? result.Enrollment.ClassSection.SectionCode : null,
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

        public static ExamDto ToExamDto(Exam exam)
        {
            var classSubject = exam.ClassSubject;

            var examDto = new ExamDto
            {
                Id = exam.Id,
                ExamTermId = exam.ExamTermId,
                ClassSubjectId = exam.ClassSubjectId,
                SubjectCode = classSubject != null ? classSubject.SubjectCode : null,
                GradeCode = classSubject != null && classSubject.AcademicClass != null ? classSubject.AcademicClass.GradeCode : null,
                ExamDate = exam.ExamDate,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                InvigilatorEmployeeId = exam.InvigilatorEmployeeId,
                InvigilatorName = exam.InvigilatorEmployee != null ? (exam.InvigilatorEmployee.FirstName + " " + exam.InvigilatorEmployee.LastName) : null,
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

        public static StudentExamMarkDto ToStudentExamMarkDto(StudentExamMark mark)
        {
            var studentExamMarkDto = new StudentExamMarkDto
            {
                Id = mark.Id,
                ExamId = mark.ExamId,
                SubjectCode = mark.Exam != null && mark.Exam.ClassSubject != null ? mark.Exam.ClassSubject.SubjectCode : null,
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
