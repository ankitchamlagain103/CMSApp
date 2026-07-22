using Application.FeeRules.Dtos;
using Domain.Entities;

namespace Application.FeeRules
{
    public static class FeeRuleMapper
    {
        public static FeeRuleDto ToDto(FeeRule rule)
        {
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
                AcademicClassGradeCode = rule.AcademicClass?.GradeCode,
                FeeCategoryCode = rule.FeeCategoryCode,
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
