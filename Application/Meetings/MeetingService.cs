using Application.Calendars;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Meetings.Commands;
using Application.Meetings.Dtos;
using Application.Meetings.Queries;
using Application.Meetings.Validators;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Meetings
{
    public class MeetingService : IMeetingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBsAdConversionService _conversionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ScheduleMeetingCommandValidator _scheduleValidator;
        private readonly UpdateMeetingCommandValidator _updateValidator;
        private readonly RespondInvitationCommandValidator _respondValidator;

        public MeetingService(
            IUnitOfWork unitOfWork,
            IBsAdConversionService conversionService,
            ICurrentUserService currentUserService,
            ScheduleMeetingCommandValidator scheduleValidator,
            UpdateMeetingCommandValidator updateValidator,
            RespondInvitationCommandValidator respondValidator)
        {
            _unitOfWork = unitOfWork;
            _conversionService = conversionService;
            _currentUserService = currentUserService;
            _scheduleValidator = scheduleValidator;
            _updateValidator = updateValidator;
            _respondValidator = respondValidator;
        }

        public async Task<CommonResponse<MeetingDto>> ScheduleMeetingAsync(ScheduleMeetingCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _scheduleValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var hostUserId = ResolveHostUserId(command.HostUserId);
            if (!hostUserId.HasValue)
            {
                var hostFailureResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.ValidationError, "The meeting host could not be resolved -- supply HostUserId or call as an authenticated user.");
                return hostFailureResponse;
            }

            DateTime adDate;
            int bsYear;
            int bsMonth;
            int bsDay;
            try
            {
                (adDate, bsYear, bsMonth, bsDay) = await ResolveMeetingDateAsync(command.IsBsScheduled, command.ScheduledAdDate, command.ScheduledBsYear, command.ScheduledBsMonth, command.ScheduledBsDay, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var conversionFailureResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return conversionFailureResponse;
            }

            var conflictExists = await _unitOfWork.Meetings.HasHostTimeConflictAsync(hostUserId.Value, adDate, command.StartTime, command.EndTime, null, cancellationToken);
            if (conflictExists)
            {
                var conflictResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.Conflict, "Scheduling conflict -- the host is already booked for an overlapping time block on that date.");
                return conflictResponse;
            }

            var meeting = new Meeting
            {
                Title = command.Title.Trim(),
                Description = command.Description,
                AdDate = adDate,
                BsYear = bsYear,
                BsMonth = bsMonth,
                BsDay = bsDay,
                StartTime = command.StartTime,
                EndTime = command.EndTime,
                IsVirtual = command.IsVirtual,
                Location = command.Location,
                HostUserId = hostUserId.Value
            };

            var normalizedEmails = NormalizeEmails(command.AttendeeEmails);
            foreach (var email in normalizedEmails)
            {
                var attendee = new MeetingAttendee
                {
                    Email = email,
                    Status = AttendeeStatus.Pending
                };

                meeting.Attendees.Add(attendee);
            }

            await _unitOfWork.Meetings.AddAsync(meeting, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var meetingDto = MeetingMapper.ToDto(meeting);
            var successResponse = CommonResponse<MeetingDto>.Success(meetingDto, "Meeting scheduled successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<MeetingDto>> GetMeetingByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var meeting = await _unitOfWork.Meetings.GetWithAttendeesAsync(id, cancellationToken);
            if (meeting == null)
            {
                var notFoundResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.NotFound, "Meeting with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var meetingDto = MeetingMapper.ToDto(meeting);
            var successResponse = CommonResponse<MeetingDto>.Success(meetingDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<MeetingDto>>> GetMeetingsAsync(GetMeetingsQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new MeetingFilter
            {
                FromAdDate = query.FromAdDate?.Date,
                ToAdDate = query.ToAdDate?.Date,
                HostUserId = query.HostUserId
            };

            var pagedMeetings = await _unitOfWork.Meetings.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var meetingDtos = new List<MeetingDto>();
            foreach (var meeting in pagedMeetings.Items)
            {
                var meetingDto = MeetingMapper.ToDto(meeting);
                meetingDtos.Add(meetingDto);
            }

            var paginatedResponse = new PaginatedResponse<MeetingDto>
            {
                Items = meetingDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedMeetings.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<MeetingDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<MeetingDto>> UpdateMeetingAsync(Guid id, UpdateMeetingCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var meeting = await _unitOfWork.Meetings.GetWithAttendeesAsync(id, cancellationToken);
            if (meeting == null)
            {
                var notFoundResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.NotFound, "Meeting with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            DateTime adDate;
            int bsYear;
            int bsMonth;
            int bsDay;
            try
            {
                (adDate, bsYear, bsMonth, bsDay) = await ResolveMeetingDateAsync(command.IsBsScheduled, command.ScheduledAdDate, command.ScheduledBsYear, command.ScheduledBsMonth, command.ScheduledBsDay, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var conversionFailureResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return conversionFailureResponse;
            }

            var conflictExists = await _unitOfWork.Meetings.HasHostTimeConflictAsync(meeting.HostUserId, adDate, command.StartTime, command.EndTime, meeting.Id, cancellationToken);
            if (conflictExists)
            {
                var conflictResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.Conflict, "Scheduling conflict -- the host is already booked for an overlapping time block on that date.");
                return conflictResponse;
            }

            meeting.Title = command.Title.Trim();
            meeting.Description = command.Description;
            meeting.AdDate = adDate;
            meeting.BsYear = bsYear;
            meeting.BsMonth = bsMonth;
            meeting.BsDay = bsDay;
            meeting.StartTime = command.StartTime;
            meeting.EndTime = command.EndTime;
            meeting.IsVirtual = command.IsVirtual;
            meeting.Location = command.Location;

            if (command.AttendeeEmails != null)
            {
                SyncAttendees(meeting, command.AttendeeEmails);
            }

            _unitOfWork.Meetings.Update(meeting);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var meetingDto = MeetingMapper.ToDto(meeting);
            var successResponse = CommonResponse<MeetingDto>.Success(meetingDto, "Meeting updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteMeetingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var meeting = await _unitOfWork.Meetings.GetByIdAsync(id, cancellationToken);
            if (meeting == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Meeting with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            // Soft delete -- the meeting disappears from every calendar query but stays for audit.
            _unitOfWork.Meetings.Remove(meeting);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Meeting cancelled successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<MeetingDto>> RespondToInvitationAsync(RespondInvitationCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _respondValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var normalizedEmail = command.Email.Trim().ToLowerInvariant();
            var attendee = await _unitOfWork.Meetings.GetAttendeeByEmailAsync(command.MeetingId, normalizedEmail, cancellationToken);
            if (attendee == null)
            {
                var notFoundResponse = CommonResponse<MeetingDto>.Fail(ResponseCodes.NotFound, "No attendee with that email is registered on the meeting.");
                return notFoundResponse;
            }

            attendee.Status = command.Status;

            // Opportunistically link the attendee row to the responding user account.
            if (!attendee.UserId.HasValue && _currentUserService.UserId.HasValue)
            {
                attendee.UserId = _currentUserService.UserId;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var meeting = await _unitOfWork.Meetings.GetWithAttendeesAsync(command.MeetingId, cancellationToken);
            var meetingDto = MeetingMapper.ToDto(meeting);
            var successResponse = CommonResponse<MeetingDto>.Success(meetingDto, "RSVP recorded successfully.");
            return successResponse;
        }

        // --- Private helpers ---

        private Guid? ResolveHostUserId(Guid? commandHostUserId)
        {
            if (commandHostUserId.HasValue && commandHostUserId.Value != Guid.Empty)
            {
                return commandHostUserId;
            }

            return _currentUserService.UserId;
        }

        private async Task<(DateTime AdDate, int BsYear, int BsMonth, int BsDay)> ResolveMeetingDateAsync(bool isBsScheduled, DateTime? adDateInput, int? bsYearInput, int? bsMonthInput, int? bsDayInput, CancellationToken cancellationToken)
        {
            if (isBsScheduled)
            {
                var adDate = await _conversionService.ConvertBsToAdAsync(bsYearInput.Value, bsMonthInput.Value, bsDayInput.Value, cancellationToken);
                return (adDate, bsYearInput.Value, bsMonthInput.Value, bsDayInput.Value);
            }

            var canonicalAdDate = adDateInput.Value.Date;
            var (bsYear, bsMonth, bsDay) = await _conversionService.ConvertAdToBsAsync(canonicalAdDate, cancellationToken);
            return (canonicalAdDate, bsYear, bsMonth, bsDay);
        }

        // Replace-sync: existing attendees keep their RSVP status, new emails join as Pending,
        // emails missing from the new list are removed. Emails are normalized to lowercase.
        private static void SyncAttendees(Meeting meeting, List<string> attendeeEmails)
        {
            var normalizedEmails = NormalizeEmails(attendeeEmails);

            var attendeesToRemove = new List<MeetingAttendee>();
            foreach (var attendee in meeting.Attendees)
            {
                if (!normalizedEmails.Contains(attendee.Email))
                {
                    attendeesToRemove.Add(attendee);
                }
            }

            foreach (var attendee in attendeesToRemove)
            {
                meeting.Attendees.Remove(attendee);
            }

            foreach (var email in normalizedEmails)
            {
                var existingAttendee = meeting.Attendees.FirstOrDefault(a => a.Email == email);
                if (existingAttendee == null)
                {
                    var attendee = new MeetingAttendee
                    {
                        Email = email,
                        Status = AttendeeStatus.Pending
                    };

                    meeting.Attendees.Add(attendee);
                }
            }
        }

        private static List<string> NormalizeEmails(List<string> emails)
        {
            var normalizedEmails = new List<string>();
            if (emails == null)
            {
                return normalizedEmails;
            }

            foreach (var email in emails)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var normalized = email.Trim().ToLowerInvariant();
                if (!normalizedEmails.Contains(normalized))
                {
                    normalizedEmails.Add(normalized);
                }
            }

            return normalizedEmails;
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
