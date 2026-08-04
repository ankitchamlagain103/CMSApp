using Application.Teachers.Dtos;

namespace Application.AcademicClasses.Dtos
{
    // Created reuses TeacherAssignmentDto (Application.Teachers.Dtos) -- a created row IS a
    // TeacherAssignment, same shape regardless of whether the entry point was the Teachers
    // feature or, as here, the AcademicClasses feature.
    public class ClassTeacherAssignmentBulkEntryResultDto
    {
        public List<TeacherAssignmentDto> Created { get; set; } = new List<TeacherAssignmentDto>();
        public List<ClassTeacherAssignmentEntrySkipDto> Skipped { get; set; } = new List<ClassTeacherAssignmentEntrySkipDto>();
    }
}
