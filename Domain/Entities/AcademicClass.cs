using Domain.Enums;

namespace Domain.Entities
{
    // One class per grade per academic year (e.g. "LKG 2026"). Sections live in the child
    // ClassSection entity; subjects attach here, so every section of the class shares the same
    // subject set (electives are still chosen per enrollment). GradeCode is a Config code
    // (TypeCode ConfigTypeCodes.Grade), validated in the service layer -- NOT a database
    // foreign key, because Config.Code is only unique per TypeCode.
    public class AcademicClass : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public RecordStatus Status { get; set; }
        public virtual AcademicYear AcademicYear { get; set; }
        public virtual ICollection<ClassSection> Sections { get; set; } = new List<ClassSection>();
        public virtual ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
    }
}
