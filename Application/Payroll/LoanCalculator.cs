using Domain.Entities;

namespace Application.Payroll
{
    // Pure repayment-progress arithmetic for EmployeeLoan -- deliberately not backed by a
    // persisted repayment ledger (see EmployeeLoan's own header comment for why). "Months
    // elapsed"/"due in period" use plain Gregorian calendar-month arithmetic from StartDate, the
    // same approximation basis MonthlyBreakdownCalculator already uses for fiscal months.
    public static class LoanCalculator
    {
        public static int ComputeInstallmentsNeeded(EmployeeLoan loan)
        {
            if (loan.EmiAmount <= 0m)
            {
                return 0;
            }

            return (int)Math.Ceiling(loan.PrincipalAmount / loan.EmiAmount);
        }

        public static int ComputeInstallmentsElapsed(EmployeeLoan loan, DateTime asOfDate)
        {
            if (asOfDate.Date < loan.StartDate.Date)
            {
                return 0;
            }

            var monthsElapsed = ((asOfDate.Year - loan.StartDate.Year) * 12) + asOfDate.Month - loan.StartDate.Month + 1;
            var installmentsNeeded = ComputeInstallmentsNeeded(loan);
            return Math.Min(Math.Max(monthsElapsed, 0), installmentsNeeded);
        }

        public static decimal ComputeAmountRepaid(EmployeeLoan loan, DateTime asOfDate)
        {
            var installmentsElapsed = ComputeInstallmentsElapsed(loan, asOfDate);
            var amountRepaid = installmentsElapsed * loan.EmiAmount;
            return Math.Min(amountRepaid, loan.PrincipalAmount);
        }

        // Whether this loan's EMI should be deducted for the fiscal month whose Gregorian period
        // starts on periodStartDate -- used to fold the EMI into the Payslip/Tax-Details monthly
        // breakdown without a persisted EmployeeSalaryDeduction row.
        public static bool IsDueInPeriod(EmployeeLoan loan, DateTime periodStartDate)
        {
            if (periodStartDate.Date < loan.StartDate.Date)
            {
                return false;
            }

            var installmentIndex = ((periodStartDate.Year - loan.StartDate.Year) * 12) + periodStartDate.Month - loan.StartDate.Month + 1;
            return installmentIndex >= 1 && installmentIndex <= ComputeInstallmentsNeeded(loan);
        }
    }
}
