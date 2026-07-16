using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds ONE example FiscalYear + a matching TaxSlab set so the payroll feature has something
    // to compute against out of the box. These are illustrative placeholder figures (the general
    // shape of Nepal's progressive individual income tax -- ~1/10/20/30/36% brackets, Couple
    // thresholds a step higher than Individual) -- NOT guaranteed to match the current government
    // of Nepal budget. Tax law changes yearly; verify/replace via
    // PUT/POST /api/fiscalyears/{id}/taxslabs before relying on this for real payroll, the same
    // caution already given to Jwt:Key/Smtp:*/Seed:* in appsettings.json. Idempotent by
    // FiscalYear.Code, safe on every boot.
    public static class PayrollSeeder
    {
        private const string SampleFiscalYearCode = "FY-SAMPLE";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
                IsCurrent = true,
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
