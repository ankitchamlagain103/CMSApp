using Domain.Enums;

namespace Domain.Entities
{
    // A section of an AcademicClass ("LKG 2026 / Section A"). Capacity lives here, not on the
    // class, because students are enrolled into a section. SectionCode is a Config code
    // (TypeCode ConfigTypeCodes.Section), validated in the service layer, not a database FK.
    public class ClassSection : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public string SectionCode { get; set; }
        public int Capacity { get; set; }
        public RecordStatus Status { get; set; }
        public virtual AcademicClass AcademicClass { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
