namespace Application.TimePeriods.Commands
{
    // Bulk class<->period mapping -- creates the cross-product of every AcademicClassId x every
    // TimePeriodId in one call (skip-list style: an already-mapped pair is reported in Skipped,
    // not an error). This is how "certain classes run different periods than certain other
    // classes" is expressed: map one set of periods to the Nursery-Five classes, a different set
    // to Six-Twelve, via two separate calls.
    public class MapClassTimePeriodsCommand
    {
        public List<Guid> AcademicClassIds { get; set; } = new List<Guid>();
        public List<Guid> TimePeriodIds { get; set; } = new List<Guid>();
    }
}
