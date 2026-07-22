namespace Application.Calendars
{
    // Thrown by BsAdConversionService for out-of-range dates and missing BsMonthLength
    // configuration. A dedicated type so calling services can catch it narrowly and turn it
    // into a ValidationError response instead of letting it surface as a 500.
    public class BsCalendarException : Exception
    {
        public BsCalendarException(string message) : base(message)
        {
        }
    }
}
