using Domain.Enums;

namespace Application.Payroll.Dtos
{
    // One SalaryComponentType/DeductionType/SalaryAdjustmentType catalog option's parsed
    // "CALCULATE_TYPE|TYPE|FREQUENCY" rule (Domain/Constants/SalaryLineCalculationModes) -- built
    // for every code whose AdditionalValue1 parses as the 3-segment format. Rate/BaseComponentCode
    // are only populated when Mode == SalaryLineCalculationModes.Percentage; a code with no entry
    // in the built map (AdditionalValue1 blank or not in the 3-segment format) keeps today's
    // fully free-form per-line ValueType/Value/FrequencyType behavior -- see
    // SalaryLineCalculationHelper.
    public class SalaryLineCalculationConfig
    {
        public string CalculateType { get; set; }
        public string Mode { get; set; }
        public PayFrequencyType Frequency { get; set; }
        public decimal? Rate { get; set; }
        public string BaseComponentCode { get; set; }
    }
}
