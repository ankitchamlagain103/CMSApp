namespace Application.Configs.Dtos
{
    public class ConfigDto
    {
        public Guid Id { get; set; }
        public int TypeCode { get; set; }
        public string Code { get; set; }
        public string Label { get; set; }
        public int Order { get; set; }
        public string AdditionalValue1 { get; set; }
        public string AdditionalValue2 { get; set; }
        public string AdditionalValue3 { get; set; }
    }
}
