using Domain.Enums;

namespace Domain.Entities
{
    // A single slot in the school's daily routine -- a teaching period or a break (Kind). This
    // replaced the earlier Config-catalog approach (ConfigTypeCodes.ExamPeriod/ClassPeriod,
    // 2026-07-30/2026-08-03) because a real relationship was needed: different classes can run
    // different period structures (e.g. shorter periods for Nursery than for Grade Ten), which a
    // flat Config option list has no way to express. See ClassTimePeriod for the class mapping,
    // and Exam.TimePeriodId/TeacherAssignment.TimePeriodId for the two consumers.
    //
    // No FK-owned children here -- TimePeriod is the definition; ClassTimePeriod (and every
    // consumer's TimePeriodId) points AT it, never the reverse. Deletion is refused while any of
    // those still reference it (TimePeriodService.DeleteTimePeriodAsync), same "refuse delete
    // while children exist" convention used throughout this codebase. Soft-deleted --
    // referencing rows keep resolving even if a period is retired from future use.
    public class TimePeriod : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public PeriodKind Kind { get; set; }
        public int Order { get; set; }
    }
}
