using Application.Common.Models;
using Application.Meetings.Commands;
using Application.Meetings.Dtos;
using Application.Meetings.Queries;

namespace Application.Meetings
{
    public interface IMeetingService
    {
        Task<CommonResponse<MeetingDto>> ScheduleMeetingAsync(ScheduleMeetingCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<MeetingDto>> GetMeetingByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<MeetingDto>>> GetMeetingsAsync(GetMeetingsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<MeetingDto>> UpdateMeetingAsync(Guid id, UpdateMeetingCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteMeetingAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<MeetingDto>> RespondToInvitationAsync(RespondInvitationCommand command, CancellationToken cancellationToken = default);
    }
}
