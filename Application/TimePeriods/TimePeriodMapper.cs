using Application.Common.Helpers;
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
        // labelsByCode (2026-08-05): Grade (1001) label map; null keeps GradeLabel at the code.
        public static ClassTimePeriodDto ToClassTimePeriodDto(ClassTimePeriod mapping, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var timePeriod = mapping.TimePeriod;
            var gradeCode = mapping.AcademicClass != null ? mapping.AcademicClass.GradeCode : null;

            var classTimePeriodDto = new ClassTimePeriodDto
            {
                Id = mapping.Id,
                AcademicClassId = mapping.AcademicClassId,
                GradeCode = gradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(labelsByCode, gradeCode),
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
