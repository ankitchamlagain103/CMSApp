using Domain.Enums;

namespace Application.PayrollRuns.Commands
{
    // A manual line added to a Draft slip (Source = Manual). Only Earning/Deduction are
    // enterable by hand -- Tax and LoanEmi lines are always machine-generated.
    public class SalarySlipLineInput
    {
        public SalaryLineType LineType { get; set; }
        public string ComponentCode { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}
