namespace Domain.Entities
{
    // A thin teaching-specific profile hanging off an Employee via a SHARED primary key --
    // Teacher.Id is always equal to its owning Employee.Id (EF's shared-PK 1:1 pattern, see
    // TeacherConfiguration). Only exists for staff who teach (EmployeeCategory Academic,
    // JobPosition Teacher/Principal/Vice Principal -- service-enforced in
    // EmployeeService.PromoteToTeacherAsync / TeacherService.CreateTeacherAsync). Identity fields
    // (name/email/phone/employee code/join date/status) all live on Employee now -- this table
    // only carries what's teaching-specific. Assignments are unchanged from before the
    // Employee/Teacher split (TeacherId FK still targets this table's Id). Qualifications and
    // Documents moved to Employee entirely on 2026-07-23 (EmployeeQualification/EmployeeDocument)
    // -- they were never actually teaching-specific (any staff member can hold a degree or need a
    // citizenship document on file), so Teacher no longer owns those collections at all.
    public class Teacher : AuditableEntity
    {
        public Guid Id { get; set; }
        public string TeachingLicenseNo { get; set; }
        public int? ExperienceYears { get; set; }
        public string Specialization { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual ICollection<TeacherAssignment> Assignments { get; set; } = new List<TeacherAssignment>();
    }
}
