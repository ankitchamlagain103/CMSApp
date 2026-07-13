using Application.AcademicYears.Dtos;
using Domain.Entities;

namespace Application.AcademicYears
{
    public static class AcademicYearMapper
    {
        public static AcademicYearDto ToDto(AcademicYear academicYear)
        {
            var academicYearDto = new AcademicYearDto
            {
                Id = academicYear.Id,
                Code = academicYear.Code,
                Name = academicYear.Name,
                StartDate = academicYear.StartDate,
                EndDate = academicYear.EndDate,
                IsCurrent = academicYear.IsCurrent,
                Status = academicYear.Status
            };

            return academicYearDto;
        }
    }
}
