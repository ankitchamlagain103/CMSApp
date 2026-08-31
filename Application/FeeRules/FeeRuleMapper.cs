using Application.Common.Helpers;
using Application.FeeRules.Dtos;
using Domain.Entities;

namespace Application.FeeRules
{
    public static class FeeRuleMapper
    {
        // labelsByCode (2026-08-05): merged Grade (1001) + FeeCategory (1010) label map; null
        // keeps both Label fields at their raw code.
        public static FeeRuleDto ToDto(FeeRule rule, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var academicClassGradeCode = rule.AcademicClass?.GradeCode;

            var ruleDto = new FeeRuleDto
            {
                Id = rule.Id,
                Code = rule.Code,
                Name = rule.Name,
                RuleType = rule.RuleType,
                TriggerStage = rule.TriggerStage,
                ValueType = rule.ValueType,
                Value = rule.Value,
                MinMonthsTogether = rule.MinMonthsTogether,
                DaysBeforeDueDate = rule.DaysBeforeDueDate,
                AcademicClassId = rule.AcademicClassId,
                AcademicClassGradeCode = academicClassGradeCode,
                AcademicClassGradeLabel = ConfigLabelHelper.Resolve(labelsByCode, academicClassGradeCode),
                FeeCategoryCode = rule.FeeCategoryCode,
                FeeCategoryLabel = ConfigLabelHelper.Resolve(labelsByCode, rule.FeeCategoryCode),
                EffectiveFrom = rule.EffectiveFrom,
                EffectiveTo = rule.EffectiveTo,
                Priority = rule.Priority,
                IsCombinable = rule.IsCombinable,
                IsActive = rule.IsActive
            };

            return ruleDto;
        }
    }
}
