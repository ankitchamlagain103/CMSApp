using Application.LeaveTypes.Dtos;
using Domain.Entities;

namespace Application.LeaveTypes
{
    public static class LeaveTypeMapper
    {
        public static LeaveTypeDto ToDto(LeaveType leaveType)
        {
            var leaveTypeDto = new LeaveTypeDto
            {
                Id = leaveType.Id,
                Name = leaveType.Name,
                DaysPerYear = leaveType.DaysPerYear,
                CarryForward = leaveType.CarryForward,
                IsPaid = leaveType.IsPaid,
                MaxConsecutiveDays = leaveType.MaxConsecutiveDays,
                MaxDaysPerWeek = leaveType.MaxDaysPerWeek,
                MaxDaysPerMonth = leaveType.MaxDaysPerMonth
            };

            return leaveTypeDto;
        }
    }
}
