namespace Application.GradeScales.Commands
{
    public class CreateGradeScaleCommand
    {
        public string Grade { get; set; }
        public decimal MinPercent { get; set; }
        public decimal MaxPercent { get; set; }
        public decimal GradePoint { get; set; }
        public string Remarks { get; set; }
    }
}
