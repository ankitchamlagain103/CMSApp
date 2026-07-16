using Domain.Enums;

namespace Application.Enrollments.Commands
{
    // ValueType/Value are optional -- when omitted, the service falls back to the ScholarshipType
    // catalog's configured default rate (Config.AdditionalValue1 = default ValueType,
    // AdditionalValue2 = default Value). Supplying them explicitly is the "individual override"
    // path -- the two-tier configuration the fee/discount system uses throughout.
    public class AddScholarshipCommand
    {
        public string ScholarshipTypeCode { get; set; }
        public AwardValueType? ValueType { get; set; }
        public decimal? Value { get; set; }
        public string Remarks { get; set; }
    }
}
