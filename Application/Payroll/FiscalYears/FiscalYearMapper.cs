using Application.Payroll.FiscalYears.Dtos;
using Domain.Entities;

namespace Application.Payroll.FiscalYears
{
    public static class FiscalYearMapper
    {
        public static FiscalYearDto ToDto(FiscalYear fiscalYear)
        {
            var fiscalYearDto = new FiscalYearDto
            {
                Id = fiscalYear.Id,
                Code = fiscalYear.Code,
                Name = fiscalYear.Name,
                StartDate = fiscalYear.StartDate,
                EndDate = fiscalYear.EndDate,
                IsCurrent = fiscalYear.IsCurrent,
                Status = fiscalYear.Status
            };

            return fiscalYearDto;
        }

        public static TaxSlabDto ToTaxSlabDto(TaxSlab taxSlab)
        {
            var taxSlabDto = new TaxSlabDto
            {
                Id = taxSlab.Id,
                FiscalYearId = taxSlab.FiscalYearId,
                AssessmentType = taxSlab.AssessmentType,
                MinAmount = taxSlab.MinAmount,
                MaxAmount = taxSlab.MaxAmount,
                TaxRate = taxSlab.TaxRate,
                SlabOrder = taxSlab.SlabOrder
            };

            return taxSlabDto;
        }
    }
}
