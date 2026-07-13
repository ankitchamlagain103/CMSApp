namespace Application.Common.Models
{
    // The one shape every dropdown endpoint in the system returns: bind "value" as the option's
    // submitted value and "label" as its display text. AdditionalValue1..3 carry optional extra
    // data per option (a color, an icon, a numeric bound, ...) and are null when unused.
    public class DropdownItemDto
    {
        public string Value { get; set; }
        public string Label { get; set; }
        public int Order { get; set; }
        public string AdditionalValue1 { get; set; }
        public string AdditionalValue2 { get; set; }
        public string AdditionalValue3 { get; set; }
    }
}
