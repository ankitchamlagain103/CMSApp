using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    // One matched Student row in a global search result -- deliberately minimal (a typeahead
    // result row, not the full profile) since GET /api/students/{id} is one click away.
    public class GlobalSearchStudentResultDto
    {
        public Guid Id { get; set; }
        public string AdmissionNo { get; set; }
        public string FullName { get; set; }
        public Gender Gender { get; set; }
        public RecordStatus Status { get; set; }
    }
}
