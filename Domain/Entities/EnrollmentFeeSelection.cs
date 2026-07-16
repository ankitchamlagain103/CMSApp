namespace Domain.Entities
{
    // Which OPTIONAL fee items (FeeStructureItem.IsOptional = true) this enrollment has opted
    // into -- e.g. this student uses Transport and Hostel but not Meal. Mandatory items apply to
    // every enrollment automatically and never need a row here. Same shape as EnrollmentSubject
    // (the elective-subject pick): pure link row, hard-deleted. FeeStructureItemId (not a category
    // string) since 2026-07-15 -- fee items are now free-named rows, not catalog codes.
    public class EnrollmentFeeSelection : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid FeeStructureItemId { get; set; }
        public virtual Enrollment Enrollment { get; set; }
        public virtual FeeStructureItem FeeStructureItem { get; set; }
    }
}
