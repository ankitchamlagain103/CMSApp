namespace Application.AcademicClasses.Commands
{
    // ClassSectionId scopes an OPTIONAL subject to one section of the class (null = offered to
    // every section). Mandatory subjects are always class-wide, so IsMandatory = true with a
    // ClassSectionId is rejected.
    public class AssignClassSubjectCommand
    {
        public string SubjectCode { get; set; }
        public bool IsMandatory { get; set; } = true;
        public int DisplayOrder { get; set; }
        public Guid? ClassSectionId { get; set; }
    }
}
