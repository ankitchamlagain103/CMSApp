using Application.PayrollRuns.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.PayrollRuns
{
    public static class PayrollRunMapper
    {
        public static PayrollRunDto ToDto(PayrollRun run, bool includeSlips)
        {
            var runDto = new PayrollRunDto
            {
                Id = run.Id,
                FiscalYearId = run.FiscalYearId,
                FiscalYearCode = run.FiscalYear?.Code,
                MonthIndex = run.MonthIndex,
                Status = run.Status,
                GeneratedTs = run.GeneratedTs,
                ApprovedTs = run.ApprovedTs,
                ApprovedBy = run.ApprovedBy,
                PaidTs = run.PaidTs,
                Remarks = run.Remarks,
                CreatedBy = run.CreatedBy,
                CreatedTs = run.CreatedTs,
                UpdatedBy = run.UpdatedBy,
                UpdatedTs = run.UpdatedTs
            };

            foreach (var slip in run.Slips)
            {
                // Cancelled slips stay visible in the Slips list but are excluded from the
                // header aggregates -- their amounts are not going to be paid.
                if (slip.Status != SalarySlipStatus.Cancelled)
                {
                    runDto.SlipCount++;
                    runDto.TotalGrossEarnings += slip.GrossEarnings;
                    runDto.TotalNetPay += slip.NetPay;
                }

                if (includeSlips)
                {
                    var slipDto = ToSlipDto(slip, includeLines: false);
                    runDto.Slips.Add(slipDto);
                }
            }

            return runDto;
        }

        public static SalarySlipDto ToSlipDto(SalarySlip slip, bool includeLines)
        {
            var slipDto = new SalarySlipDto
            {
                Id = slip.Id,
                SlipNo = slip.SlipNo,
                PayrollRunId = slip.PayrollRunId,
                EmployeeId = slip.EmployeeId,
                EmployeeSalaryId = slip.EmployeeSalaryId,
                Status = slip.Status,
                PeriodStartDate = slip.PeriodStartDate,
                PeriodEndDate = slip.PeriodEndDate,
                MonthDays = slip.MonthDays,
                PayDays = slip.PayDays,
                UnpaidLeaveDays = slip.UnpaidLeaveDays,
                GrossEarnings = slip.GrossEarnings,
                TotalDeductions = slip.TotalDeductions,
                TaxAmount = slip.TaxAmount,
                NetPay = slip.NetPay,
                Remarks = slip.Remarks
            };

            var employee = slip.Employee;
            if (employee != null)
            {
                slipDto.EmployeeCode = employee.EmployeeCode;

                var nameParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(employee.FirstName))
                {
                    nameParts.Add(employee.FirstName);
                }

                if (!string.IsNullOrWhiteSpace(employee.MiddleName))
                {
                    nameParts.Add(employee.MiddleName);
                }

                if (!string.IsNullOrWhiteSpace(employee.LastName))
                {
                    nameParts.Add(employee.LastName);
                }

                slipDto.EmployeeName = string.Join(" ", nameParts);
            }

            if (includeLines)
            {
                foreach (var line in slip.Lines)
                {
                    var lineDto = ToSlipLineDto(line);
                    slipDto.Lines.Add(lineDto);
                }
            }

            return slipDto;
        }

        public static SalarySlipLineDto ToSlipLineDto(SalarySlipLine line)
        {
            var lineDto = new SalarySlipLineDto
            {
                Id = line.Id,
                LineType = line.LineType,
                Source = line.Source,
                ComponentCode = line.ComponentCode,
                Description = line.Description,
                Amount = line.Amount
            };

            return lineDto;
        }
    }
}
