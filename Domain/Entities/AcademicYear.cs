using Domain.Enums;

namespace Domain.Entities
{
    public class AcademicYear : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public RecordStatus Status { get; set; }
        public virtual ICollection<AcademicClass> Classes { get; set; } = new List<AcademicClass>();
    }
}
