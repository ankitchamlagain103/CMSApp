using Application.TimePeriods.Dtos;
using Domain.Entities;

namespace Application.TimePeriods
{
    public static class TimePeriodMapper
    {
        public static TimePeriodDto ToDto(TimePeriod timePeriod)
        {
            var timePeriodDto = new TimePeriodDto
            {
                Id = timePeriod.Id,
                Name = timePeriod.Name,
                StartTime = timePeriod.StartTime,
                EndTime = timePeriod.EndTime,
                Kind = timePeriod.Kind,
                Order = timePeriod.Order
            };

            return timePeriodDto;
        }

        // Expects the mapping's AcademicClass and TimePeriod navigations to be loaded.
        public static ClassTimePeriodDto ToClassTimePeriodDto(ClassTimePeriod mapping)
        {
            var timePeriod = mapping.TimePeriod;

            var classTimePeriodDto = new ClassTimePeriodDto
            {
                Id = mapping.Id,
                AcademicClassId = mapping.AcademicClassId,
                GradeCode = mapping.AcademicClass != null ? mapping.AcademicClass.GradeCode : null,
                TimePeriodId = mapping.TimePeriodId,
                TimePeriodName = timePeriod != null ? timePeriod.Name : null,
                StartTime = timePeriod != null ? timePeriod.StartTime : default,
                EndTime = timePeriod != null ? timePeriod.EndTime : default,
                Kind = timePeriod != null ? timePeriod.Kind : default
            };

            return classTimePeriodDto;
        }
    }
}
