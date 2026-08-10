namespace Domain.Enums
{
    // System-generated notification categories -- bounded and code-controlled (unlike a Config
    // catalog), since each value corresponds to a specific event the backend itself raises, not
    // admin-configurable data.
    public enum NotificationType
    {
        General = 0,
        LeaveRequestSubmitted = 1,
        LeaveManagerApproved = 2,
        LeaveManagerRejected = 3,
        LeaveHrApproved = 4,
        LeaveHrRejected = 5,
        BirthdayReminder = 6,
        WorkAnniversaryReminder = 7,
        DocumentVerified = 8,
        DocumentRejected = 9,
        QualificationVerified = 10,
        QualificationRejected = 11
    }
}
