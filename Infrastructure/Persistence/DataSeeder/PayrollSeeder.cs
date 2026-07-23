using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds fiscal years + their TaxSlab sets so payroll has something to compute against out of
    // the box. FY-SAMPLE is an illustrative placeholder (~1/10/20/30/36% brackets, Couple
    // thresholds a step higher than Individual). "2084/85" (added 2026-07-20) is the fiscal
    // year the user actually configured and exercised end-to-end against this app's
    // TaxCalculator/PayrollRun refresh path (see the "Payroll fixes" 2026-07-18 round-2 write-up
    // in CLAUDE.md) -- its slabs are copied verbatim from that already-verified data. Both are
    // still illustrative in the sense that Nepal's tax law changes yearly by budget -- verify/
    // replace via PUT/POST /api/fiscalyears/{id}/taxslabs before relying on either for real
    // payroll, the same caution already given to Jwt:Key/Smtp:*/Seed:* in appsettings.json. Each
    // fiscal year is seeded independently, idempotent by FiscalYear.Code, safe on every boot.
    public static class PayrollSeeder
    {
        private const string SampleFiscalYearCode = "FY-SAMPLE";
        private const string Fy2084_85Code = "2084/85";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedSampleFiscalYearAsync(dbContext);
            await SeedFy2084_85Async(dbContext);
        }

        private static async Task SeedSampleFiscalYearAsync(ApplicationDbContext dbContext)
        {
            var fiscalYearExists = await dbContext.Set<FiscalYear>().AnyAsync(year => year.Code == SampleFiscalYearCode);
            if (fiscalYearExists)
            {
                return;
            }

            var fiscalYear = new FiscalYear
            {
                Code = SampleFiscalYearCode,
                Name = "Sample Fiscal Year (verify slabs before real use)",
                StartDate = new DateTime(DateTime.UtcNow.Year, 7, 16),
                EndDate = new DateTime(DateTime.UtcNow.Year + 1, 7, 15),
                // Not the current year -- "2084/85" below is the real one. Only one FiscalYear
                // should be IsCurrent at a time (the invariant FiscalYearService enforces on
                // every create/update via UnsetCurrentYearsAsync); seeders write straight
                // through the DbContext and must keep that true by construction instead.
                IsCurrent = false,
                Status = RecordStatus.Active,
                // The configurable "C" in the retirement-fund "least of three" exemption rule --
                // 500,000 in the reference payslip example. Same "verify before real use" caution
                // as the tax slabs below.
                RetirementExemptionCapAmount = 500_000m
            };

            AddSlab(fiscalYear, TaxAssessmentType.Individual, 0m, 500_000m, 0.01m, 1);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 500_000m, 700_000m, 0.10m, 2);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 700_000m, 1_000_000m, 0.20m, 3);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 1_000_000m, 2_000_000m, 0.30m, 4);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 2_000_000m, null, 0.36m, 5);

            AddSlab(fiscalYear, TaxAssessmentType.Couple, 0m, 600_000m, 0.01m, 1);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 600_000m, 800_000m, 0.10m, 2);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 800_000m, 1_100_000m, 0.20m, 3);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 1_100_000m, 2_000_000m, 0.30m, 4);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 2_000_000m, null, 0.36m, 5);

            dbContext.Set<FiscalYear>().Add(fiscalYear);
            await dbContext.SaveChangesAsync();
        }

        // Fiscal year 2084/85 BS (2026-07-17 to 2027-07-15 AD): 1/10/20/27/29% brackets,
        // identical thresholds for Individual and Couple. RetirementExemptionCapAmount is seeded
        // at 500,000 (a working default) -- note a dev database that already has this fiscal
        // year from manual entry may still have it at 0 (a known flagged gap, see CLAUDE.md's
        // "Two fiscal years, same tax" note); this method only ever creates the row, so it never
        // touches an existing one -- fix a live row via PUT /api/fiscalyears/{id} instead.
        private static async Task SeedFy2084_85Async(ApplicationDbContext dbContext)
        {
            var fiscalYearExists = await dbContext.Set<FiscalYear>().AnyAsync(year => year.Code == Fy2084_85Code);
            if (fiscalYearExists)
            {
                return;
            }

            var fiscalYear = new FiscalYear
            {
                Code = Fy2084_85Code,
                Name = "FY 2084/85",
                StartDate = new DateTime(2026, 7, 17),
                EndDate = new DateTime(2027, 7, 15),
                IsCurrent = true,
                Status = RecordStatus.Active,
                RetirementExemptionCapAmount = 500_000m
            };

            // Boundaries are contiguous (each slab's MinAmount == the previous slab's MaxAmount,
            // first slab starts at 0), same convention as FY-SAMPLE above -- Calculate(...)'s
            // "if (annualTaxableIncome <= slab.MinAmount) continue;" + Math.Min(income, ceiling)
            // combo already prevents double-taxing the exact boundary rupee, so no "+1" offset is
            // needed between slabs. An earlier version of this data used a "+1" gap (1 /
            // 1,000,001 / 1,500,001 / ...), copied verbatim from a live dev-DB fiscal year that
            // had been hand-entered that way -- it silently understated taxable income by exactly
            // 1 rupee at every bracket transition (verified 2026-07-22 against a real HRMS
            // system's payslip, which taxes the full contiguous width per bracket with no gap).
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 0m, 1_000_000m, 0.01m, 1);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 1_000_000m, 1_500_000m, 0.10m, 2);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 1_500_000m, 2_500_000m, 0.20m, 3);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 2_500_000m, 4_000_000m, 0.27m, 4);
            AddSlab(fiscalYear, TaxAssessmentType.Individual, 4_000_000m, null, 0.29m, 5);

            AddSlab(fiscalYear, TaxAssessmentType.Couple, 0m, 1_000_000m, 0.01m, 1);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 1_000_000m, 1_500_000m, 0.10m, 2);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 1_500_000m, 2_500_000m, 0.20m, 3);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 2_500_000m, 4_000_000m, 0.27m, 4);
            AddSlab(fiscalYear, TaxAssessmentType.Couple, 4_000_000m, null, 0.29m, 5);

            dbContext.Set<FiscalYear>().Add(fiscalYear);
            await dbContext.SaveChangesAsync();
        }

        private static void AddSlab(FiscalYear fiscalYear, TaxAssessmentType assessmentType, decimal minAmount, decimal? maxAmount, decimal taxRate, int slabOrder)
        {
            var taxSlab = new TaxSlab
            {
                AssessmentType = assessmentType,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                TaxRate = taxRate,
                SlabOrder = slabOrder
            };

            fiscalYear.TaxSlabs.Add(taxSlab);
        }
    }
}
