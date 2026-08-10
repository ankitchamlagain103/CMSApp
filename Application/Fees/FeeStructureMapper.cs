using Application.Common.Helpers;
using Application.Fees.Dtos;
using Domain.Entities;

namespace Application.Fees
{
    public static class FeeStructureMapper
    {
        // Expects the fee structure's AcademicClass and Items navigations to be loaded (the
        // repository includes both). labelsByCode is the merged FeeCategory (1010) + Grade (1001)
        // label map (extended 2026-08-05); null keeps every Label field at the code itself.
        public static FeeStructureDto ToDto(FeeStructure feeStructure, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var academicClass = feeStructure.AcademicClass;
            var gradeCode = academicClass != null ? academicClass.GradeCode : null;

            var feeStructureDto = new FeeStructureDto
            {
                Id = feeStructure.Id,
                AcademicClassId = feeStructure.AcademicClassId,
                AcademicYearId = academicClass != null ? academicClass.AcademicYearId : Guid.Empty,
                GradeCode = gradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(labelsByCode, gradeCode),
                Status = feeStructure.Status
            };

            foreach (var item in feeStructure.Items)
            {
                var itemDto = ToItemDto(item, labelsByCode);
                feeStructureDto.Items.Add(itemDto);
            }

            return feeStructureDto;
        }

        public static FeeStructureItemDto ToItemDto(FeeStructureItem item, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var itemDto = new FeeStructureItemDto
            {
                Id = item.Id,
                FeeStructureId = item.FeeStructureId,
                FeeCategoryCode = item.FeeCategoryCode,
                FeeCategoryLabel = ConfigLabelHelper.Resolve(labelsByCode, item.FeeCategoryCode),
                Amount = item.Amount,
                FrequencyType = item.FrequencyType,
                InstallmentCount = item.InstallmentCount,
                IsOptional = item.IsOptional,
                IsRefundable = item.IsRefundable
            };

            return itemDto;
        }
    }
}
