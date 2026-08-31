using Application.Common.Helpers;
using Application.Employees.Dtos;
using Application.Payroll;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Application.Employees
{
    public static class EmployeeMapper
    {
        // orgLabelsByCode (2026-08-05) resolves EmployeeCategory/JobPosition/Branch/Province/
        // EmployeeLevel/District/LocalLevel codes -- see EmployeeService.LoadOrgLabelMapAsync;
        // null/omitted leaves every XxxLabel field null (same graceful-degradation the
        // SalaryComponent/Deduction mappers already established).
        public static EmployeeDto ToDto(Employee employee, IReadOnlyDictionary<string, string> orgLabelsByCode = null)
        {
            var employeeDto = new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                EmployeeCode = employee.EmployeeCode,
                FirstName = employee.FirstName,
                MiddleName = employee.MiddleName,
                LastName = employee.LastName,
                Gender = employee.Gender,
                DateOfBirth = employee.DateOfBirth,
                Email = employee.Email,
                Phone = employee.Phone,
                JoinDate = employee.JoinDate,
                EmployeeCategoryCode = employee.EmployeeCategoryCode,
                EmployeeCategoryLabel = ConfigLabelHelper.Resolve(orgLabelsByCode, employee.EmployeeCategoryCode),
                JobPositionCode = employee.JobPositionCode,
                JobPositionLabel = ConfigLabelHelper.Resolve(orgLabelsByCode, employee.JobPositionCode),
                EmploymentStatus = employee.EmploymentStatus,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                PaymentMode = employee.PaymentMode,
                PanNumber = employee.PanNumber,
                ProvidentFundNumber = employee.ProvidentFundNumber,
                SsfNumber = employee.SsfNumber,
                CitNumber = employee.CitNumber,
                GratuityNumber = employee.GratuityNumber,
                BranchCode = employee.BranchCode,
                BranchLabel = ConfigLabelHelper.Resolve(orgLabelsByCode, employee.BranchCode),
                ProvinceCode = employee.ProvinceCode,
                ProvinceLabel = ConfigLabelHelper.Resolve(orgLabelsByCode, employee.ProvinceCode),
                LevelCode = employee.LevelCode,
                LevelLabel = ConfigLabelHelper.Resolve(orgLabelsByCode, employee.LevelCode),
                ManagerId = employee.ManagerId,
                DistrictCode = employee.DistrictCode,
                DistrictLabel = ConfigLabelHelper.Resolve(orgLabelsByCode, employee.DistrictCode),
                LocalLevelCode = employee.LocalLevelCode,
                LocalLevelLabel = ConfigLabelHelper.Resolve(orgLabelsByCode, employee.LocalLevelCode),
                WardNo = employee.WardNo,
                ManagerName = employee.Manager != null ? BuildFullName(employee.Manager.FirstName, employee.Manager.MiddleName, employee.Manager.LastName) : null,
                HasPhoto = !string.IsNullOrWhiteSpace(employee.PhotoPath),
                TeachingLicenseNo = employee.TeachingLicenseNo,
                ExperienceYears = employee.ExperienceYears,
                Specialization = employee.Specialization,
                IsTeachingStaff = EmployeeRoleHelper.IsTeachingStaff(employee.EmployeeCategoryCode, employee.JobPositionCode),
                CreatedBy = employee.CreatedBy,
                CreatedTs = employee.CreatedTs,
                UpdatedBy = employee.UpdatedBy,
                UpdatedTs = employee.UpdatedTs
            };

            return employeeDto;
        }

        // Expects the salary's Components/Deductions/InsurancePremiums navigations to be loaded.
        // labelsByCode (2026-07-19): merged SalaryComponentType/DeductionType/InsuranceType
        // Config label map; null keeps every *Label field at its code.
        public static EmployeeSalaryDto ToSalaryDto(EmployeeSalary salary, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var salaryDto = new EmployeeSalaryDto
            {
                Id = salary.Id,
                EmployeeId = salary.EmployeeId,
                EffectiveFromDate = salary.EffectiveFromDate,
                AssessmentType = salary.AssessmentType
            };

            foreach (var component in salary.Components)
            {
                salaryDto.Components.Add(ToComponentDto(component, labelsByCode));
            }

            foreach (var deduction in salary.Deductions)
            {
                salaryDto.Deductions.Add(ToDeductionDto(deduction, labelsByCode));
            }

            foreach (var premium in salary.InsurancePremiums)
            {
                salaryDto.InsurancePremiums.Add(ToInsurancePremiumDto(premium, labelsByCode));
            }

            return salaryDto;
        }

        public static SalaryComponentDto ToComponentDto(EmployeeSalaryComponent component, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var componentDto = new SalaryComponentDto
            {
                Id = component.Id,
                EmployeeSalaryId = component.EmployeeSalaryId,
                ComponentCode = component.ComponentCode,
                ComponentLabel = ConfigLabelHelper.Resolve(labelsByCode, component.ComponentCode),
                ValueType = component.ValueType,
                Value = component.Value,
                FrequencyType = component.FrequencyType,
                IsTaxable = component.IsTaxable,
                IsRetirementContribution = component.IsRetirementContribution
            };

            return componentDto;
        }

        public static SalaryDeductionDto ToDeductionDto(EmployeeSalaryDeduction deduction, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var deductionDto = new SalaryDeductionDto
            {
                Id = deduction.Id,
                EmployeeSalaryId = deduction.EmployeeSalaryId,
                DeductionCode = deduction.DeductionCode,
                DeductionLabel = ConfigLabelHelper.Resolve(labelsByCode, deduction.DeductionCode),
                ValueType = deduction.ValueType,
                Value = deduction.Value,
                FrequencyType = deduction.FrequencyType,
                IsRetirementContribution = deduction.IsRetirementContribution
            };

            return deductionDto;
        }

        // 2026-07-23: the two adapters AddSalaryLineAsync uses to present whichever of
        // ToComponentDto/ToDeductionDto it actually built as the unified SalaryLineDto shape.
        public static SalaryLineDto ToLineDto(SalaryComponentDto componentDto)
        {
            var lineDto = new SalaryLineDto
            {
                Id = componentDto.Id,
                EmployeeSalaryId = componentDto.EmployeeSalaryId,
                Code = componentDto.ComponentCode,
                Label = componentDto.ComponentLabel,
                CalculateType = SalaryLineCalculateTypes.Addition,
                ValueType = componentDto.ValueType,
                Value = componentDto.Value,
                FrequencyType = componentDto.FrequencyType,
                IsTaxable = componentDto.IsTaxable,
                IsRetirementContribution = componentDto.IsRetirementContribution
            };

            return lineDto;
        }

        public static SalaryLineDto ToLineDto(SalaryDeductionDto deductionDto)
        {
            var lineDto = new SalaryLineDto
            {
                Id = deductionDto.Id,
                EmployeeSalaryId = deductionDto.EmployeeSalaryId,
                Code = deductionDto.DeductionCode,
                Label = deductionDto.DeductionLabel,
                CalculateType = SalaryLineCalculateTypes.Deduction,
                ValueType = deductionDto.ValueType,
                Value = deductionDto.Value,
                FrequencyType = deductionDto.FrequencyType,
                IsTaxable = null,
                IsRetirementContribution = deductionDto.IsRetirementContribution
            };

            return lineDto;
        }

        // Qualifications and Documents (2026-07-23, moved here from TeacherMapper -- neither
        // concept is teaching-specific).
        public static EmployeeQualificationDto ToQualificationDto(EmployeeQualification qualification, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var qualificationDto = new EmployeeQualificationDto
            {
                Id = qualification.Id,
                EmployeeId = qualification.EmployeeId,
                QualificationCode = qualification.QualificationCode,
                QualificationLabel = ConfigLabelHelper.Resolve(labelsByCode, qualification.QualificationCode),
                CourseName = qualification.CourseName,
                Institution = qualification.Institution,
                CompletionYear = qualification.CompletionYear,
                Score = qualification.Score,
                Remarks = qualification.Remarks,
                VerificationStatus = qualification.VerificationStatus,
                VerificationRemarks = qualification.VerificationRemarks,
                VerifiedTs = qualification.VerifiedTs,
                VerifiedBy = qualification.VerifiedBy
            };

            return qualificationDto;
        }

        public static EmployeeDocumentDto ToDocumentDto(EmployeeDocument document, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var documentDto = new EmployeeDocumentDto
            {
                Id = document.Id,
                EmployeeId = document.EmployeeId,
                DocumentTypeCode = document.DocumentTypeCode,
                DocumentTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, document.DocumentTypeCode),
                DocumentName = document.DocumentName,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSizeBytes = document.FileSizeBytes,
                ValidUntil = document.ValidUntil,
                Remarks = document.Remarks,
                UploadedTs = document.CreatedTs,
                VerificationStatus = document.VerificationStatus,
                VerificationRemarks = document.VerificationRemarks,
                VerifiedTs = document.VerifiedTs,
                VerifiedBy = document.VerifiedBy
            };

            return documentDto;
        }

        // Leave management (2026-07-23).

        public static EmployeeLeaveBalanceDto ToLeaveBalanceDto(EmployeeLeaveBalance balance)
        {
            var balanceDto = new EmployeeLeaveBalanceDto
            {
                Id = balance.Id,
                EmployeeId = balance.EmployeeId,
                LeaveTypeId = balance.LeaveTypeId,
                LeaveTypeName = balance.LeaveType != null ? balance.LeaveType.Name : null,
                FiscalYearId = balance.FiscalYearId,
                FiscalYearCode = balance.FiscalYear != null ? balance.FiscalYear.Code : null,
                Allocated = balance.Allocated,
                Used = balance.Used,
                Pending = balance.Pending,
                Balance = balance.Balance
            };

            return balanceDto;
        }

        // Expects LeaveType/Employee/SubstituteEmployee navigations to be loaded where present;
        // falls back to null names otherwise. EffectiveStatus follows LeaveRequest's own doc
        // comment: HrStatus is authoritative once decided; a still-Pending HrStatus alongside a
        // Rejected ManagerStatus shows as a provisional Rejected (HR can still override).
        public static LeaveRequestDto ToLeaveRequestDto(LeaveRequest request)
        {
            var requestDto = new LeaveRequestDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                EmployeeName = request.Employee != null ? BuildFullName(request.Employee.FirstName, request.Employee.MiddleName, request.Employee.LastName) : null,
                LeaveTypeId = request.LeaveTypeId,
                LeaveTypeName = request.LeaveType != null ? request.LeaveType.Name : null,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Days = request.Days,
                Reason = request.Reason,
                SubstituteEmployeeId = request.SubstituteEmployeeId,
                SubstituteEmployeeName = request.SubstituteEmployee != null ? BuildFullName(request.SubstituteEmployee.FirstName, request.SubstituteEmployee.MiddleName, request.SubstituteEmployee.LastName) : null,
                IsEmergency = request.IsEmergency,
                ManagerStatus = request.ManagerStatus,
                ManagerRemarks = request.ManagerRemarks,
                ManagerDecisionTs = request.ManagerDecisionTs,
                ManagerDecisionBy = request.ManagerDecisionBy,
                HrStatus = request.HrStatus,
                HrRemarks = request.HrRemarks,
                HrDecisionTs = request.HrDecisionTs,
                HrDecisionBy = request.HrDecisionBy,
                EffectiveStatus = ResolveEffectiveLeaveStatus(request.ManagerStatus, request.HrStatus),
                HasAttachment = !string.IsNullOrWhiteSpace(request.AttachmentPath),
                AttachmentFileName = request.AttachmentFileName
            };

            foreach (var substitute in request.Substitutes)
            {
                requestDto.Substitutes.Add(ToLeaveSubstituteDto(substitute));
            }

            return requestDto;
        }

        public static LeaveSubstituteDto ToLeaveSubstituteDto(LeaveSubstitute substitute)
        {
            var substituteDto = new LeaveSubstituteDto
            {
                Id = substitute.Id,
                LeaveRequestId = substitute.LeaveRequestId,
                EmployeeId = substitute.EmployeeId,
                EmployeeName = substitute.Employee != null ? BuildFullName(substitute.Employee.FirstName, substitute.Employee.MiddleName, substitute.Employee.LastName) : null,
                Responsibility = substitute.Responsibility
            };

            return substituteDto;
        }

        private static LeaveApprovalStatus ResolveEffectiveLeaveStatus(LeaveApprovalStatus managerStatus, LeaveApprovalStatus hrStatus)
        {
            if (hrStatus == LeaveApprovalStatus.Approved || hrStatus == LeaveApprovalStatus.Rejected)
            {
                return hrStatus;
            }

            if (managerStatus == LeaveApprovalStatus.Rejected)
            {
                return LeaveApprovalStatus.Rejected;
            }

            return LeaveApprovalStatus.Pending;
        }

        public static InsurancePremiumDto ToInsurancePremiumDto(EmployeeInsurancePremium premium, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var premiumDto = new InsurancePremiumDto
            {
                Id = premium.Id,
                EmployeeSalaryId = premium.EmployeeSalaryId,
                InsuranceTypeCode = premium.InsuranceTypeCode,
                InsuranceTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, premium.InsuranceTypeCode),
                AnnualPremiumAmount = premium.AnnualPremiumAmount
            };

            return premiumDto;
        }

        public static EmployeeLoanDto ToLoanDto(EmployeeLoan loan, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var asOfDate = DateTime.UtcNow;
            var amountRepaid = LoanCalculator.ComputeAmountRepaid(loan, asOfDate);

            var loanDto = new EmployeeLoanDto
            {
                Id = loan.Id,
                EmployeeId = loan.EmployeeId,
                LoanTypeCode = loan.LoanTypeCode,
                LoanTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, loan.LoanTypeCode),
                PrincipalAmount = loan.PrincipalAmount,
                EmiAmount = loan.EmiAmount,
                RequestedDate = loan.RequestedDate,
                StartDate = loan.StartDate,
                Status = loan.Status,
                Remarks = loan.Remarks,
                AmountRepaid = amountRepaid,
                RemainingBalance = loan.PrincipalAmount - amountRepaid,
                IsFullyRepaid = amountRepaid >= loan.PrincipalAmount
            };

            return loanDto;
        }

        public static SalaryAdjustmentDto ToSalaryAdjustmentDto(SalaryAdjustment adjustment, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var adjustmentDto = new SalaryAdjustmentDto
            {
                Id = adjustment.Id,
                EmployeeId = adjustment.EmployeeId,
                FiscalYearId = adjustment.FiscalYearId,
                MonthIndex = adjustment.MonthIndex,
                AdjustmentTypeCode = adjustment.AdjustmentTypeCode,
                AdjustmentTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, adjustment.AdjustmentTypeCode),
                Direction = adjustment.Direction,
                ValueType = adjustment.ValueType,
                Value = adjustment.Value,
                Quantity = adjustment.Quantity,
                Remarks = adjustment.Remarks,
                Status = adjustment.Status,
                AppliedSalarySlipId = adjustment.AppliedSalarySlipId
            };

            return adjustmentDto;
        }

        // Small standalone helper (not shared with EmployeeService.BuildFullName -- mappers stay
        // self-contained rather than reaching into a service class) for ManagerName above.
        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                nameParts.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                nameParts.Add(middleName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                nameParts.Add(lastName);
            }

            var fullName = string.Join(" ", nameParts);
            return fullName;
        }
    }
}
