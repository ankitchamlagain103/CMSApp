namespace Domain.Entities
{
    // A thin teaching-specific profile hanging off an Employee via a SHARED primary key --
    // Teacher.Id is always equal to its owning Employee.Id (EF's shared-PK 1:1 pattern, see
    // TeacherConfiguration). Only exists for staff who teach (EmployeeCategory Academic,
    // JobPosition Teacher/Principal/Vice Principal -- service-enforced in
    // EmployeeService.PromoteToTeacherAsync / TeacherService.CreateTeacherAsync). Identity fields
    // (name/email/phone/employee code/join date/status) all live on Employee now -- this table
    // only carries what's teaching-specific. Qualifications/Assignments/Documents are
    // deliberately UNCHANGED from before this split: their TeacherId FK still targets this
    // table's Id, which now simply also happens to be an Employee id.
    public class Teacher : AuditableEntity
    {
        public Guid Id { get; set; }
        public string TeachingLicenseNo { get; set; }
        public int? ExperienceYears { get; set; }
        public string Specialization { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual ICollection<TeacherQualification> Qualifications { get; set; } = new List<TeacherQualification>();
        public virtual ICollection<TeacherAssignment> Assignments { get; set; } = new List<TeacherAssignment>();
        public virtual ICollection<TeacherDocument> Documents { get; set; } = new List<TeacherDocument>();
    }
}
