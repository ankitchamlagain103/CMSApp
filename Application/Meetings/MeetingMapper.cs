using Application.Meetings.Dtos;
using Domain.Entities;

namespace Application.Meetings
{
    public static class MeetingMapper
    {
        public static MeetingDto ToDto(Meeting meeting)
        {
            var meetingDto = new MeetingDto
            {
                Id = meeting.Id,
                Title = meeting.Title,
                Description = meeting.Description,
                AdDate = meeting.AdDate,
                BsYear = meeting.BsYear,
                BsMonth = meeting.BsMonth,
                BsDay = meeting.BsDay,
                StartTime = meeting.StartTime,
                EndTime = meeting.EndTime,
                IsVirtual = meeting.IsVirtual,
                Location = meeting.Location,
                HostUserId = meeting.HostUserId
            };

            foreach (var attendee in meeting.Attendees.OrderBy(a => a.Email))
            {
                var attendeeDto = new MeetingAttendeeDto
                {
                    Id = attendee.Id,
                    UserId = attendee.UserId,
                    Email = attendee.Email,
                    Status = attendee.Status
                };

                meetingDto.Attendees.Add(attendeeDto);
            }

            return meetingDto;
        }
    }
}
