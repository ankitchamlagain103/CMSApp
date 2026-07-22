namespace Application.Common.Helpers
{
    // Postgres 'timestamp with time zone' columns reject DateTime.Kind=Unspecified (Npgsql 7+:
    // "only UTC is supported"). Every DateTime deserialized from a JSON request body comes back
    // Kind=Unspecified (no offset in the payload), and these fields are all calendar dates with
    // no time-of-day meaning (payment date, due date, effective-from/to, payroll period bounds)
    // -- same category as FiscalYear/EmployeeLoan/Employee dates, which sidestep this by mapping
    // to a plain 'date' column instead. Where the entity column is timestamptz, reinterpret
    // (never shift) the value as UTC right before it's assigned.
    public static class DateTimeHelper
    {
        public static DateTime AsUtcDate(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
        }

        public static DateTime? AsUtcDate(DateTime? value)
        {
            return value.HasValue ? AsUtcDate(value.Value) : null;
        }
    }
}
