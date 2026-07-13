namespace Application.AcademicYears.Dtos
{
    public class CloneStructureResultDto
    {
        public Guid SourceAcademicYearId { get; set; }
        public Guid TargetAcademicYearId { get; set; }
        public int ClassesCreated { get; set; }
        public int SectionsCreated { get; set; }
        public int SubjectsCreated { get; set; }

        // Grades already present in the target year -- left untouched.
        public List<string> SkippedGradeCodes { get; set; } = new List<string>();
    }
}
