using Application.Employees.Dtos;
using Application.Exams.Dtos;

namespace Application.Students.Dtos
{
    // The Student Portal's composite "My Dashboard" widget -- current class, fee due summary,
    // most recent exam results, and upcoming events, in one call. Mirrors EmployeeDashboardDto's
    // shape/reasoning for the Staff Portal's own My Dashboard.
    public class StudentDashboardDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }

        // Null when the student has no active (Status == Enrolled) enrollment anywhere.
        public StudentCurrentEnrollmentDto CurrentEnrollment { get; set; }

        // Sum of (NetAmount - PaidAmount) across every open (Generated/Pending/PartiallyPaid)
        // invoice for the active enrollment -- same formula used throughout FeeInvoiceService's
        // own statement/outstanding calculations.
        public decimal TotalOutstandingAmount { get; set; }
        public DateTime? NextFeeDueDate { get; set; }
        public int OpenInvoiceCount { get; set; }

        // Every StudentResult recorded for the active enrollment, newest exam term first, capped
        // to a dashboard-sized list.
        public List<StudentResultDto> RecentResults { get; set; } = new List<StudentResultDto>();

        // School-wide holidays/internal events/festivals plus this student's own next birthday,
        // within a fixed forward window -- same UpcomingEventDto shape EmployeeDashboardDto uses.
        public List<UpcomingEventDto> UpcomingEvents { get; set; } = new List<UpcomingEventDto>();
    }
}
