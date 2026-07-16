using Application.Fees.Dtos;
using Domain.Entities;

namespace Application.Fees
{
    public static class FeeStructureMapper
    {
        // Expects the fee structure's AcademicClass and Items navigations to be loaded (the
        // repository includes both).
        public static FeeStructureDto ToDto(FeeStructure feeStructure)
        {
            var academicClass = feeStructure.AcademicClass;

            var feeStructureDto = new FeeStructureDto
            {
                Id = feeStructure.Id,
                AcademicClassId = feeStructure.AcademicClassId,
                AcademicYearId = academicClass != null ? academicClass.AcademicYearId : Guid.Empty,
                GradeCode = academicClass != null ? academicClass.GradeCode : null,
                Status = feeStructure.Status
            };

            foreach (var item in feeStructure.Items)
            {
                var itemDto = ToItemDto(item);
                feeStructureDto.Items.Add(itemDto);
            }

            return feeStructureDto;
        }

        public static FeeStructureItemDto ToItemDto(FeeStructureItem item)
        {
            var itemDto = new FeeStructureItemDto
            {
                Id = item.Id,
                FeeStructureId = item.FeeStructureId,
                FeeCategoryCode = item.FeeCategoryCode,
                Amount = item.Amount,
                FrequencyType = item.FrequencyType,
                IsOptional = item.IsOptional,
                IsRefundable = item.IsRefundable
            };

            return itemDto;
        }
    }
}
