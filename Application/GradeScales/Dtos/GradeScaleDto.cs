namespace Application.GradeScales.Dtos
{
    public class GradeScaleDto
    {
        public Guid Id { get; set; }
        public string Grade { get; set; }
        public decimal MinPercent { get; set; }
        public decimal MaxPercent { get; set; }
        public decimal GradePoint { get; set; }
        public string Remarks { get; set; }
    }
}
