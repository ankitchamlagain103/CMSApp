namespace Domain.Enums
{
    public enum CalendarEventType
    {
        Note = 0,
        PublicHoliday = 1,
        InternalEvent = 2,

        // 2026-07-23: added for the Leave Management / Employee Profile "Upcoming Events" work --
        // lets an admin pin a specific person's birthday onto the shared calendar view (distinct
        // from the profile page's own live-computed "next birthday" figure, which needs no
        // persisted row at all -- see CalendarEvent.StudentId/EmployeeId).
        StudentBirthday = 3,
        EmployeeBirthday = 4,

        // 2026-07-28: auto-created by ExamService when an ExamSchedule is scheduled (Exam
        // Management module) -- see ExamSchedule.CalendarEventId.
        Exam = 5
    }
}
