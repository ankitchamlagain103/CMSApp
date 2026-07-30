namespace Application.GradeScales.Commands
{
    // Grade itself is identity-like and immutable -- same convention as UpdateLeaveTypeCommand
    // allowing Name to change but every other feature keeping its natural key fixed on update
    // where re-keying would mean creating a new row instead. Grade is the natural key here, so
    // unlike LeaveType.Name it is NOT editable -- delete and recreate for a rename.
    public class UpdateGradeScaleCommand
    {
        public decimal MinPercent { get; set; }
        public decimal MaxPercent { get; set; }
        public decimal GradePoint { get; set; }
        public string Remarks { get; set; }
    }
}
