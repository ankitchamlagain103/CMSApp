namespace Application.AcademicYears.Commands
{
    // Copies the source year's classes, sections, and subject mappings into the target year
    // (the {id} in the route). Grades that already exist in the target are skipped, so the
    // operation is safe to run against a partially set-up year.
    public class CloneYearStructureCommand
    {
        public Guid SourceAcademicYearId { get; set; }
    }
}
