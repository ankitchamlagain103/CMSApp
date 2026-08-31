using Domain.Enums;

namespace Domain.Entities
{
    // A macro examination period within an AcademicYear (e.g. "First Terminal", "Final Exam").
    // Identity-bearing (Code is unique, referenced by future result/promotion records), hence
    // soft-deleted like AcademicYear/AcademicClass. PublishResult gates whether the term's
    // (future) results are visible outside the admin/teacher side.
    public class ExamTerm : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid AcademicYearId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool PublishResult { get; set; }
        public ExamTermStatus Status { get; set; }

        public virtual AcademicYear AcademicYear { get; set; }
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
