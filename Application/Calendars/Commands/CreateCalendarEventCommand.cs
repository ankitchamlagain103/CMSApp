using Domain.Enums;

namespace Application.Calendars.Commands
{
    // The event's date may be given in either calendar: IsBsDate=false -> AdDate is required
    // and the BS fields are ignored; IsBsDate=true -> BsYear/BsMonth/BsDay are required and
    // AdDate is ignored. The service computes and stores the other calendar's fields.
    public class CreateCalendarEventCommand
    {
        public string Title { get; set; }
        public CalendarEventType EventType { get; set; }
        public bool IsBsDate { get; set; }
        public DateTime? AdDate { get; set; }
        public int? BsYear { get; set; }
        public int? BsMonth { get; set; }
        public int? BsDay { get; set; }
        public string Description { get; set; }
        public string IconKey { get; set; }
        public string ColorCode { get; set; }
        public string Language { get; set; } = "en";
        public bool IsActive { get; set; } = true;
    }
}
