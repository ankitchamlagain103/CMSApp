using Domain.Enums;

namespace Application.Enrollments.Commands
{
    // Student/class are deliberately immutable -- moving a student is a new enrollment (the old
    // one becomes Transferred/Withdrawn), which preserves the history row.
    public class UpdateEnrollmentCommand
    {
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public EnrollmentStatus Status { get; set; }

        // Student-profile checkboxes for optional fees (transportation, hostel, ...) with the
        // same three-way semantics as UpdateUserCommand.RoleIds: null = leave selections
        // unchanged, [] = clear all opt-ins, non-empty = replace-sync to exactly this set.
        // Only affects future invoice generation -- already-generated invoices are snapshots.
        public List<Guid> OptionalFeeStructureItemIds { get; set; }
    }
}
