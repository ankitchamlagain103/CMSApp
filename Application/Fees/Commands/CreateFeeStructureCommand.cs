namespace Application.Fees.Commands
{
    // Creates a class's fee structure header and every submitted line item in one
    // SaveChangesAsync -- Items may be empty (header-only, items added later one at a time via the
    // item sub-resource), but the whole point of this shape is to let an admin set up a full class
    // fee list without one create call per category.
    public class CreateFeeStructureCommand
    {
        public Guid AcademicClassId { get; set; }
        public List<FeeStructureItemInput> Items { get; set; } = new List<FeeStructureItemInput>();
    }
}
