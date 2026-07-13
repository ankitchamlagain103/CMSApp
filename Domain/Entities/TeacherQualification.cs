namespace Domain.Entities
{
    // One qualification record per row (a teacher holds many). QualificationCode is a Config code
    // (ConfigTypeCodes.TeacherQualification, e.g. BACHELORS/MASTERS), validated in the service
    // layer, not a database FK. Hard-deleted (child record of Teacher).
    public class TeacherQualification : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public string QualificationCode { get; set; }
        public string CourseName { get; set; }
        public string Institution { get; set; }
        public int? CompletionYear { get; set; }
        public string Score { get; set; }
        public string Remarks { get; set; }
        public virtual Teacher Teacher { get; set; }
    }
}
