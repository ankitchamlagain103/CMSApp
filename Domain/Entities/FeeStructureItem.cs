using Domain.Enums;

namespace Domain.Entities
{
    // A single fee line item on a class's fee structure. FeeCategoryCode is a Config code
    // (ConfigTypeCodes.FeeCategory), validated in the service layer, not a database FK -- same
    // convention as SubjectCode/GradeCode (2026-07-15, reverted back from a brief free-text "Name"
    // experiment: an admin can still create a whole class fee list in one call via the header+items
    // shape, but each item must reference a known category -- add a new one via POST /api/configs
    // first if it isn't in the catalog yet). Unique per FeeStructure (a class can't charge the same
    // category twice). IsOptional marks items that don't apply to every student by default -- an
    // enrollment opts in via EnrollmentFeeSelection, the same "mandatory applies automatically,
    // optional needs an explicit pick" shape as ClassSubject/EnrollmentSubject. IsRefundable flags
    // deposit-style items that shouldn't be counted as a cost in fee summaries.
    public class FeeStructureItem : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid FeeStructureId { get; set; }
        public string FeeCategoryCode { get; set; }
        public decimal Amount { get; set; }
        public FeeFrequencyType FrequencyType { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRefundable { get; set; }
        public virtual FeeStructure FeeStructure { get; set; }
    }
}
