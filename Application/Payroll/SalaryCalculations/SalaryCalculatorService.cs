using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Employees;
using Application.Employees.Commands;
using Application.Payroll.Dtos;
using Application.Payroll.SalaryCalculations.Commands;
using Application.Payroll.SalaryCalculations.Dtos;
using Application.Payroll.SalaryCalculations.Validators;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Payroll.SalaryCalculations
{
    // HR salary-structuring calculator: fixes one figure (net / gross / CTC) and solves the
    // rest -- Basic (pinned amount or % of gross), allowance, SSF employee deduction + employer
    // contribution (rates from Config catalog 1018), optional festival bonus / CIT savings /
    // capped insurance premiums, and TDS via the fiscal year's slabs. It builds a synthetic
    // compensation plan and runs it through the same TaxCalculator the payroll run uses, so a
    // structure accepted from here produces exactly the numbers the calculator promised.
    // CalculateStructureAsync persists nothing; AssignStructureAsync persists the suggested
    // lines as a real salary revision through IEmployeeService.AddSalaryAsync.
    public class SalaryCalculatorService : ISalaryCalculatorService
    {
        private const decimal DefaultBasicPercentOfGross = 60m;
        private const decimal DefaultSsfEmployeeRatePercent = 11m;
        private const decimal DefaultSsfEmployerRatePercent = 20m;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmployeeService _employeeService;
        private readonly CalculateSalaryStructureCommandValidator _calculateValidator;
        private readonly AssignSalaryStructureCommandValidator _assignValidator;

        public SalaryCalculatorService(
            IUnitOfWork unitOfWork,
            IEmployeeService employeeService,
            CalculateSalaryStructureCommandValidator calculateValidator,
            AssignSalaryStructureCommandValidator assignValidator)
        {
            _unitOfWork = unitOfWork;
            _employeeService = employeeService;
            _calculateValidator = calculateValidator;
            _assignValidator = assignValidator;
        }

        public async Task<CommonResponse<SalaryStructureCalculationDto>> CalculateStructureAsync(CalculateSalaryStructureCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _calculateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalaryStructureCalculationDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var fiscalYear = command.FiscalYearId.HasValue
                ? await _unitOfWork.FiscalYears.GetByIdAsync(command.FiscalYearId.Value, cancellationToken)
                : await _unitOfWork.FiscalYears.GetCurrentYearAsync(cancellationToken);
            if (fiscalYear == null)
            {
                var noFiscalYearResponse = CommonResponse<SalaryStructureCalculationDto>.Fail(ResponseCodes.NotFound, command.FiscalYearId.HasValue ? "Fiscal year with id '" + command.FiscalYearId.Value + "' was not found." : "No fiscal year is marked as current.");
                return noFiscalYearResponse;
            }

            var taxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, command.AssessmentType, cancellationToken);
            if (taxSlabs.Count == 0)
            {
                var noSlabsResponse = CommonResponse<SalaryStructureCalculationDto>.Fail(ResponseCodes.ValidationError, "Fiscal year '" + fiscalYear.Code + "' has no tax slabs configured for assessment type '" + command.AssessmentType + "'.");
                return noSlabsResponse;
            }

            var ssfEmployeeRatePercent = 0m;
            var ssfEmployerRatePercent = 0m;
            if (command.IncludeSsf)
            {
                var ssfRateConfigs = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.SsfRate, cancellationToken);
                ssfEmployeeRatePercent = ResolveSsfRate(ssfRateConfigs, SsfShareCodes.EmployeeShare, DefaultSsfEmployeeRatePercent);
                ssfEmployerRatePercent = ResolveSsfRate(ssfRateConfigs, SsfShareCodes.EmployerShare, DefaultSsfEmployerRatePercent);
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);

            decimal grossPayment;
            if (command.Basis == SalaryCalculationBasis.GrossPayment)
            {
                grossPayment = command.Amount;
            }
            else if (command.Basis == SalaryCalculationBasis.Ctc)
            {
                if (command.BasicSalaryAmount.HasValue)
                {
                    // Basic is pinned, so the employer SSF load is a fixed amount -- subtract it.
                    var fixedEmployerSsf = command.BasicSalaryAmount.Value * (ssfEmployerRatePercent / 100m);
                    grossPayment = command.Amount - fixedEmployerSsf;
                }
                else
                {
                    // CTC = gross + employerRate% of (basicPct% of gross) -- linear in gross.
                    var basicPercent = command.BasicPercentOfGross ?? DefaultBasicPercentOfGross;
                    var employerLoadFactor = 1m + (ssfEmployerRatePercent / 100m) * (basicPercent / 100m);
                    grossPayment = command.Amount / employerLoadFactor;
                }
            }
            else
            {
                grossPayment = SolveGrossForNet(command, ssfEmployeeRatePercent, ssfEmployerRatePercent, insuranceTypeCaps, fiscalYear, taxSlabs);
            }

            if (grossPayment <= 0m)
            {
                var impossibleResponse = CommonResponse<SalaryStructureCalculationDto>.Fail(ResponseCodes.ValidationError, "No positive gross payment satisfies these inputs -- the pinned Basic salary's SSF load alone exceeds the target amount.");
                return impossibleResponse;
            }

            if (command.BasicSalaryAmount.HasValue && command.BasicSalaryAmount.Value > grossPayment)
            {
                var basicTooLargeResponse = CommonResponse<SalaryStructureCalculationDto>.Fail(ResponseCodes.ValidationError, "BasicSalaryAmount (" + command.BasicSalaryAmount.Value + ") exceeds the computed gross payment (" + Math.Round(grossPayment, 2) + ") -- lower the Basic or raise the target amount.");
                return basicTooLargeResponse;
            }

            var calculation = BuildCalculation(command, grossPayment, ssfEmployeeRatePercent, ssfEmployerRatePercent, insuranceTypeCaps, fiscalYear, taxSlabs);
            calculation.Basis = command.Basis;
            calculation.FiscalYearId = fiscalYear.Id;
            calculation.FiscalYearCode = fiscalYear.Code;
            calculation.AssessmentType = command.AssessmentType;

            var successResponse = CommonResponse<SalaryStructureCalculationDto>.Success(calculation);
            return successResponse;
        }

        // Persists a calculation as a real salary revision for one employee, through the same
        // service the manual Add Salary Revision form uses -- so date-conflict checks, catalog
        // code validation, and audit stamping all apply identically.
        public async Task<CommonResponse<SalaryStructureAssignResultDto>> AssignStructureAsync(AssignSalaryStructureCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _assignValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalaryStructureAssignResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var calculationResponse = await CalculateStructureAsync(command, cancellationToken);
            if (calculationResponse.Data == null)
            {
                var calculationFailureResponse = CommonResponse<SalaryStructureAssignResultDto>.Fail(calculationResponse.ResponseCode, calculationResponse.ResponseMessage);
                return calculationFailureResponse;
            }

            var calculation = calculationResponse.Data;

            var addSalaryCommand = new AddEmployeeSalaryCommand
            {
                EffectiveFromDate = command.EffectiveFromDate,
                AssessmentType = command.AssessmentType
            };

            foreach (var suggestedComponent in calculation.SuggestedComponents)
            {
                addSalaryCommand.Components.Add(new SalaryComponentInput
                {
                    ComponentCode = suggestedComponent.Code,
                    ValueType = suggestedComponent.ValueType,
                    Value = suggestedComponent.Value,
                    FrequencyType = suggestedComponent.FrequencyType,
                    IsTaxable = suggestedComponent.IsTaxable,
                    IsRetirementContribution = suggestedComponent.IsRetirementContribution
                });
            }

            foreach (var suggestedDeduction in calculation.SuggestedDeductions)
            {
                addSalaryCommand.Deductions.Add(new SalaryDeductionInput
                {
                    DeductionCode = suggestedDeduction.Code,
                    ValueType = suggestedDeduction.ValueType,
                    Value = suggestedDeduction.Value,
                    FrequencyType = suggestedDeduction.FrequencyType,
                    IsRetirementContribution = suggestedDeduction.IsRetirementContribution
                });
            }

            foreach (var suggestedPremium in calculation.SuggestedInsurancePremiums)
            {
                addSalaryCommand.InsurancePremiums.Add(new InsurancePremiumInput
                {
                    InsuranceTypeCode = suggestedPremium.InsuranceTypeCode,
                    AnnualPremiumAmount = suggestedPremium.AnnualPremiumAmount
                });
            }

            var addSalaryResponse = await _employeeService.AddSalaryAsync(command.EmployeeId, addSalaryCommand, cancellationToken);
            if (addSalaryResponse.Data == null)
            {
                var addSalaryFailureResponse = CommonResponse<SalaryStructureAssignResultDto>.Fail(addSalaryResponse.ResponseCode, addSalaryResponse.ResponseMessage);
                return addSalaryFailureResponse;
            }

            var assignResult = new SalaryStructureAssignResultDto
            {
                Calculation = calculation,
                Salary = addSalaryResponse.Data
            };

            var successResponse = CommonResponse<SalaryStructureAssignResultDto>.Success(assignResult, "Salary structure assigned -- a new revision effective " + command.EffectiveFromDate.ToString("yyyy-MM-dd") + " was created.");
            return successResponse;
        }

        // Net-to-gross has no closed form (the tax is a progressive slab walk), but net is
        // strictly increasing in gross, so a plain binary search converges fast; 80 halvings on
        // a bounded range is far below a paisa of error. The lower bound may go below the pinned
        // Basic -- BuildCalculation tolerates a transiently negative allowance during the
        // search; the caller rejects a final result whose Basic exceeds gross.
        private static decimal SolveGrossForNet(CalculateSalaryStructureCommand command, decimal ssfEmployeeRatePercent, decimal ssfEmployerRatePercent, IReadOnlyDictionary<string, InsuranceCapConfig> insuranceTypeCaps, FiscalYear fiscalYear, IReadOnlyList<TaxSlab> taxSlabs)
        {
            var targetNetPayment = command.Amount;
            var lowerGross = targetNetPayment;
            var upperGross = targetNetPayment * 3m + 10000m;

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var upperCalculation = BuildCalculation(command, upperGross, ssfEmployeeRatePercent, ssfEmployerRatePercent, insuranceTypeCaps, fiscalYear, taxSlabs);
                if (upperCalculation.NetPayment >= targetNetPayment)
                {
                    break;
                }

                upperGross *= 2m;
            }

            for (var iteration = 0; iteration < 80; iteration++)
            {
                var midGross = (lowerGross + upperGross) / 2m;
                var midCalculation = BuildCalculation(command, midGross, ssfEmployeeRatePercent, ssfEmployerRatePercent, insuranceTypeCaps, fiscalYear, taxSlabs);

                if (midCalculation.NetPayment < targetNetPayment)
                {
                    lowerGross = midGross;
                }
                else
                {
                    upperGross = midGross;
                }
            }

            return upperGross;
        }

        private static SalaryStructureCalculationDto BuildCalculation(CalculateSalaryStructureCommand command, decimal grossPayment, decimal ssfEmployeeRatePercent, decimal ssfEmployerRatePercent, IReadOnlyDictionary<string, InsuranceCapConfig> insuranceTypeCaps, FiscalYear fiscalYear, IReadOnlyList<TaxSlab> taxSlabs)
        {
            var basicPercentOfGross = command.BasicPercentOfGross ?? DefaultBasicPercentOfGross;
            var basicSalary = command.BasicSalaryAmount ?? grossPayment * (basicPercentOfGross / 100m);
            var otherAllowance = grossPayment - basicSalary;
            var ssfEmployeeDeduction = basicSalary * (ssfEmployeeRatePercent / 100m);
            var ssfEmployerContribution = basicSalary * (ssfEmployerRatePercent / 100m);
            var annualBonusAmount = command.AnnualBonusAmount ?? 0m;
            var monthlyCitAmount = command.MonthlyCitAmount ?? 0m;

            // Synthetic plan mirroring what the suggested lines would persist. The employer SSF
            // contribution is modeled the standard Nepali way: a taxable benefit that also counts
            // toward the retirement-fund exemption (and is offset by an equal fund-remittance
            // deduction on the payslip, so it never lands in cash). The festival bonus is a
            // OneTime taxable component; CIT is an employee-funded retirement deduction.
            var components = new List<EmployeeSalaryComponent>();
            components.Add(new EmployeeSalaryComponent
            {
                ComponentCode = SalaryComponentCodes.Basic,
                ValueType = AwardValueType.FixedAmount,
                Value = basicSalary,
                FrequencyType = PayFrequencyType.Monthly,
                IsTaxable = true,
                IsRetirementContribution = false
            });

            if (otherAllowance > 0m)
            {
                components.Add(new EmployeeSalaryComponent
                {
                    ComponentCode = SalaryComponentCodes.OtherAllowance,
                    ValueType = AwardValueType.FixedAmount,
                    Value = otherAllowance,
                    FrequencyType = PayFrequencyType.Monthly,
                    IsTaxable = true,
                    IsRetirementContribution = false
                });
            }

            if (ssfEmployerContribution > 0m)
            {
                components.Add(new EmployeeSalaryComponent
                {
                    ComponentCode = SalaryComponentCodes.SsfContribution,
                    ValueType = AwardValueType.Percentage,
                    Value = ssfEmployerRatePercent,
                    FrequencyType = PayFrequencyType.Monthly,
                    IsTaxable = true,
                    IsRetirementContribution = true
                });
            }

            if (annualBonusAmount > 0m)
            {
                components.Add(new EmployeeSalaryComponent
                {
                    ComponentCode = SalaryComponentCodes.FestivalBonus,
                    ValueType = AwardValueType.FixedAmount,
                    Value = annualBonusAmount,
                    FrequencyType = PayFrequencyType.OneTime,
                    IsTaxable = true,
                    IsRetirementContribution = false
                });
            }

            var deductions = new List<EmployeeSalaryDeduction>();
            if (ssfEmployeeDeduction > 0m)
            {
                deductions.Add(new EmployeeSalaryDeduction
                {
                    DeductionCode = SalaryDeductionCodes.SsfDeduction,
                    ValueType = AwardValueType.Percentage,
                    Value = ssfEmployeeRatePercent,
                    FrequencyType = PayFrequencyType.Monthly,
                    IsRetirementContribution = true
                });
            }

            if (monthlyCitAmount > 0m)
            {
                deductions.Add(new EmployeeSalaryDeduction
                {
                    DeductionCode = SalaryDeductionCodes.CitDeduction,
                    ValueType = AwardValueType.FixedAmount,
                    Value = monthlyCitAmount,
                    FrequencyType = PayFrequencyType.Monthly,
                    IsRetirementContribution = true
                });
            }

            var insurancePremiums = new List<EmployeeInsurancePremium>();
            if (command.AnnualLifeInsurancePremium.HasValue && command.AnnualLifeInsurancePremium.Value > 0m)
            {
                insurancePremiums.Add(new EmployeeInsurancePremium
                {
                    InsuranceTypeCode = InsuranceTypeCodes.Life,
                    AnnualPremiumAmount = command.AnnualLifeInsurancePremium.Value
                });
            }

            if (command.AnnualHealthInsurancePremium.HasValue && command.AnnualHealthInsurancePremium.Value > 0m)
            {
                insurancePremiums.Add(new EmployeeInsurancePremium
                {
                    InsuranceTypeCode = InsuranceTypeCodes.Health,
                    AnnualPremiumAmount = command.AnnualHealthInsurancePremium.Value
                });
            }

            var taxCalculation = TaxCalculator.CalculateFromSalary(
                components,
                deductions,
                insurancePremiums,
                insuranceTypeCaps,
                fiscalYear.RetirementExemptionCapAmount,
                taxSlabs);

            var monthlyTax = taxCalculation.MonthlyTax;
            var netPayment = grossPayment - ssfEmployeeDeduction - monthlyCitAmount - monthlyTax;
            var ctc = grossPayment + ssfEmployerContribution;

            var calculation = new SalaryStructureCalculationDto
            {
                BasicPercentOfGross = basicPercentOfGross,
                SsfEmployeeRatePercent = ssfEmployeeRatePercent,
                SsfEmployerRatePercent = ssfEmployerRatePercent,
                BasicSalary = Math.Round(basicSalary, 2),
                OtherAllowance = Math.Round(otherAllowance, 2),
                GrossPayment = Math.Round(grossPayment, 2),
                SsfEmployeeDeduction = Math.Round(ssfEmployeeDeduction, 2),
                SsfEmployerContribution = Math.Round(ssfEmployerContribution, 2),
                MonthlyCitDeduction = Math.Round(monthlyCitAmount, 2),
                MonthlyTax = Math.Round(monthlyTax, 2),
                NetPayment = Math.Round(netPayment, 2),
                Ctc = Math.Round(ctc, 2),
                AnnualBonusAmount = Math.Round(annualBonusAmount, 2),
                AnnualGrossPayment = Math.Round(grossPayment * 12m + annualBonusAmount, 2),
                AnnualNetPayment = Math.Round(netPayment * 12m + annualBonusAmount, 2),
                AnnualCtc = Math.Round(ctc * 12m + annualBonusAmount, 2),
                TaxCalculation = taxCalculation
            };

            foreach (var component in components)
            {
                calculation.SuggestedComponents.Add(new SuggestedSalaryLineDto
                {
                    Code = component.ComponentCode,
                    ValueType = component.ValueType,
                    Value = component.ValueType == AwardValueType.FixedAmount ? Math.Round(component.Value, 2) : component.Value,
                    FrequencyType = component.FrequencyType,
                    IsTaxable = component.IsTaxable,
                    IsRetirementContribution = component.IsRetirementContribution
                });
            }

            foreach (var deduction in deductions)
            {
                calculation.SuggestedDeductions.Add(new SuggestedSalaryLineDto
                {
                    Code = deduction.DeductionCode,
                    ValueType = deduction.ValueType,
                    Value = deduction.ValueType == AwardValueType.FixedAmount ? Math.Round(deduction.Value, 2) : deduction.Value,
                    FrequencyType = deduction.FrequencyType,
                    IsTaxable = false,
                    IsRetirementContribution = deduction.IsRetirementContribution
                });
            }

            foreach (var premium in insurancePremiums)
            {
                calculation.SuggestedInsurancePremiums.Add(new SuggestedInsurancePremiumDto
                {
                    InsuranceTypeCode = premium.InsuranceTypeCode,
                    AnnualPremiumAmount = premium.AnnualPremiumAmount
                });
            }

            return calculation;
        }

        // Config catalog 1018 (SsfRate): the share's percentage lives in AdditionalValue1.
        // Missing row or unparseable value falls back to the statutory default rather than
        // failing the calculation.
        private static decimal ResolveSsfRate(IReadOnlyList<Config> ssfRateConfigs, string shareCode, decimal fallbackRatePercent)
        {
            foreach (var ssfRateConfig in ssfRateConfigs)
            {
                if (ssfRateConfig.Code == shareCode && decimal.TryParse(ssfRateConfig.AdditionalValue1, out var ratePercent))
                {
                    return ratePercent;
                }
            }

            return fallbackRatePercent;
        }

        // Same Config-catalog read as EmployeeService/PayrollRunService: InsuranceType options
        // (catalog 1015) carry their Nepal tax-deduction cap in AdditionalValue1.
        private async Task<Dictionary<string, InsuranceCapConfig>> BuildInsuranceTypeCapsAsync(CancellationToken cancellationToken)
        {
            var insuranceTypeConfigs = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.InsuranceType, cancellationToken);
            return InsuranceCapHelper.BuildCapMap(insuranceTypeConfigs);
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
