namespace Domain.Enums
{
    // A TimePeriod row's kind -- a teaching slot vs. a non-teaching break. Both live in the same
    // catalog (see TimePeriod's own doc comment) so a routine view can render the full day.
    public enum PeriodKind
    {
        Period = 0,
        Break = 1
    }
}
