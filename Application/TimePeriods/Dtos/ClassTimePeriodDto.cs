using Domain.Enums;

namespace Application.TimePeriods.Dtos
{
    // TimePeriodName/StartTime/EndTime/Kind are flattened in from the mapped TimePeriod (same
    // convention as EnrollmentDto's flattened GradeCode/SectionCode) so a class's routine screen
    // doesn't need a second lookup per row.
    public class ClassTimePeriodDto
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public Guid TimePeriodId { get; set; }
        public string TimePeriodName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public PeriodKind Kind { get; set; }
    }
}
