namespace Application.Exams.Commands
{
    // Scope narrows like FeeInvoiceService.GenerateAsync's academicClassId/classSectionId pair --
    // both optional, both combinable, omit both to cover the whole exam term.
    public class GenerateExamResultsCommand
    {
        public Guid ExamTermId { get; set; }
        public Guid? AcademicClassId { get; set; }
        public Guid? ClassSectionId { get; set; }
    }
}
