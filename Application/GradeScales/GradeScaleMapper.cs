using Application.GradeScales.Dtos;
using Domain.Entities;

namespace Application.GradeScales
{
    public static class GradeScaleMapper
    {
        public static GradeScaleDto ToDto(GradeScale gradeScale)
        {
            var gradeScaleDto = new GradeScaleDto
            {
                Id = gradeScale.Id,
                Grade = gradeScale.Grade,
                MinPercent = gradeScale.MinPercent,
                MaxPercent = gradeScale.MaxPercent,
                GradePoint = gradeScale.GradePoint,
                Remarks = gradeScale.Remarks
            };

            return gradeScaleDto;
        }
    }
}
