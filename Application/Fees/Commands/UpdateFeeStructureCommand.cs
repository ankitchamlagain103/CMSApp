using Domain.Enums;

namespace Application.Fees.Commands
{
    // AcademicClassId is immutable -- it identifies the header. Amount/frequency/optional/
    // refundable now live per-item; this only toggles the whole class fee structure's Status.
    public class UpdateFeeStructureCommand
    {
        public RecordStatus Status { get; set; }
    }
}
