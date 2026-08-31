using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class EmployeeRepository : Repository<Employee, Guid>, IEmployeeRepository
    {
        public EmployeeRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Employee>> GetPagedByFilterAsync(EmployeeFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Employee> employeesQuery = DbSet;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchPattern = "%" + filter.Search.Trim() + "%";
                employeesQuery = employeesQuery.Where(employee =>
                    EF.Functions.ILike(employee.FirstName, searchPattern)
                    || EF.Functions.ILike(employee.LastName, searchPattern)
                    || EF.Functions.ILike(employee.EmployeeCode, searchPattern));
            }

            if (!string.IsNullOrWhiteSpace(filter.Phone))
            {
                var phonePattern = "%" + filter.Phone.Trim() + "%";
                employeesQuery = employeesQuery.Where(employee => EF.Functions.ILike(employee.Phone, phonePattern));
            }

            if (!string.IsNullOrWhiteSpace(filter.EmployeeCategoryCode))
            {
                employeesQuery = employeesQuery.Where(employee => employee.EmployeeCategoryCode == filter.EmployeeCategoryCode);
            }

            if (!string.IsNullOrWhiteSpace(filter.JobPositionCode))
            {
                employeesQuery = employeesQuery.Where(employee => employee.JobPositionCode == filter.JobPositionCode);
            }

            if (!string.IsNullOrWhiteSpace(filter.QualificationCode))
            {
                // Ported from the removed TeacherRepository (2026-08-06) -- no more
                // Teacher.Employee indirection, joins straight on this employee's own Qualifications.
                employeesQuery = employeesQuery.Where(employee => employee.Qualifications.Any(qualification => qualification.QualificationCode == filter.QualificationCode));
            }

            if (filter.EmploymentStatus.HasValue)
            {
                employeesQuery = employeesQuery.Where(employee => employee.EmploymentStatus == filter.EmploymentStatus.Value);
            }

            if (filter.Gender.HasValue)
            {
                employeesQuery = employeesQuery.Where(employee => employee.Gender == filter.Gender.Value);
            }

            if (filter.FromDate.HasValue || filter.ToDate.HasValue)
            {
                employeesQuery = ApplyDateRange(employeesQuery, filter);
            }

            var totalCount = await employeesQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await employeesQuery
                .OrderBy(employee => employee.FirstName)
                .ThenBy(employee => employee.LastName)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Employee>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        // CreatedDate compares against the audit column; JoinDate compares against the employee's
        // own JoinDate (ToDate is inclusive of the whole day).
        private static IQueryable<Employee> ApplyDateRange(IQueryable<Employee> query, EmployeeFilter filter)
        {
            if (filter.DateField == EmployeeDateField.JoinDate)
            {
                if (filter.FromDate.HasValue)
                {
                    query = query.Where(employee => employee.JoinDate.HasValue && employee.JoinDate.Value >= filter.FromDate.Value.Date);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(employee => employee.JoinDate.HasValue && employee.JoinDate.Value < filter.ToDate.Value.Date.AddDays(1));
                }

                return query;
            }

            if (filter.FromDate.HasValue)
            {
                var fromTs = new DateTimeOffset(filter.FromDate.Value.Date, TimeSpan.Zero);
                query = query.Where(employee => employee.CreatedTs >= fromTs);
            }

            if (filter.ToDate.HasValue)
            {
                var toTs = new DateTimeOffset(filter.ToDate.Value.Date.AddDays(1), TimeSpan.Zero);
                query = query.Where(employee => employee.CreatedTs < toTs);
            }

            return query;
        }

        public async Task<Employee> GetByIdWithManagerAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var employee = await DbSet
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return employee;
        }

        public async Task<bool> EmployeeCodeExistsAsync(string employeeCode, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            var exists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(employee => employee.EmployeeCode == employeeCode, cancellationToken);

            return exists;
        }

        public async Task<IReadOnlyList<string>> GetEmployeeCodesByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var employeeCodes = await DbSet
                .IgnoreQueryFilters()
                .Where(employee => employee.EmployeeCode.StartsWith(prefix))
                .Select(employee => employee.EmployeeCode)
                .ToListAsync(cancellationToken);

            return employeeCodes;
        }

        // Self-service (2026-08-06) -- resolves "which Employee am I" from the caller's own
        // ApplicationUser id, for the "Me" endpoints in EmployeesController.
        public async Task<Employee> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var employee = await DbSet
                .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

            return employee;
        }

        public async Task<bool> UserIdExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var exists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(employee => employee.UserId == userId, cancellationToken);

            return exists;
        }

        public async Task<bool> HasSalariesAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var hasSalaries = await DbContext.Set<EmployeeSalary>()
                .AnyAsync(salary => salary.EmployeeId == employeeId, cancellationToken);

            return hasSalaries;
        }

        public async Task<IReadOnlyList<EmployeeSalary>> GetSalaryHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var salaries = await DbContext.Set<EmployeeSalary>()
                .Where(salary => salary.EmployeeId == employeeId)
                .OrderByDescending(salary => salary.EffectiveFromDate)
                .ToListAsync(cancellationToken);

            return salaries;
        }

        public async Task<EmployeeSalary> GetCurrentSalaryAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            // "Current" = the latest revision by EffectiveFromDate, not necessarily today's date --
            // a future-dated raise that hasn't kicked in yet still counts as the newest row.
            var currentSalary = await DbContext.Set<EmployeeSalary>()
                .Include(salary => salary.Components)
                .Include(salary => salary.Deductions)
                .Include(salary => salary.InsurancePremiums)
                .Where(salary => salary.EmployeeId == employeeId)
                .OrderByDescending(salary => salary.EffectiveFromDate)
                .FirstOrDefaultAsync(cancellationToken);

            return currentSalary;
        }

        public async Task<EmployeeSalary> GetSalaryByIdAsync(Guid salaryId, CancellationToken cancellationToken = default)
        {
            var salary = await DbContext.Set<EmployeeSalary>()
                .FirstOrDefaultAsync(s => s.Id == salaryId, cancellationToken);

            return salary;
        }

        public async Task<EmployeeSalary> GetSalaryWithLineItemsAsync(Guid salaryId, CancellationToken cancellationToken = default)
        {
            var salary = await DbContext.Set<EmployeeSalary>()
                .Include(s => s.Components)
                .Include(s => s.Deductions)
                .Include(s => s.InsurancePremiums)
                .FirstOrDefaultAsync(s => s.Id == salaryId, cancellationToken);

            return salary;
        }

        public async Task<bool> SalaryExistsForDateAsync(Guid employeeId, DateTime effectiveFromDate, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            var exists = await DbContext.Set<EmployeeSalary>()
                .IgnoreQueryFilters()
                .AnyAsync(salary => salary.EmployeeId == employeeId && salary.EffectiveFromDate == effectiveFromDate, cancellationToken);

            return exists;
        }

        public async Task AddSalaryAsync(EmployeeSalary salary, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeSalary>().AddAsync(salary, cancellationToken);
        }

        public async Task<EmployeeSalaryComponent> GetSalaryComponentByIdAsync(Guid componentId, CancellationToken cancellationToken = default)
        {
            var component = await DbContext.Set<EmployeeSalaryComponent>()
                .FirstOrDefaultAsync(c => c.Id == componentId, cancellationToken);

            return component;
        }

        public async Task AddSalaryComponentAsync(EmployeeSalaryComponent component, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeSalaryComponent>().AddAsync(component, cancellationToken);
        }

        public void RemoveSalaryComponent(EmployeeSalaryComponent component)
        {
            DbContext.Set<EmployeeSalaryComponent>().Remove(component);
        }

        public async Task<EmployeeSalaryDeduction> GetSalaryDeductionByIdAsync(Guid deductionId, CancellationToken cancellationToken = default)
        {
            var deduction = await DbContext.Set<EmployeeSalaryDeduction>()
                .FirstOrDefaultAsync(d => d.Id == deductionId, cancellationToken);

            return deduction;
        }

        public async Task AddSalaryDeductionAsync(EmployeeSalaryDeduction deduction, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeSalaryDeduction>().AddAsync(deduction, cancellationToken);
        }

        public void RemoveSalaryDeduction(EmployeeSalaryDeduction deduction)
        {
            DbContext.Set<EmployeeSalaryDeduction>().Remove(deduction);
        }

        public async Task<EmployeeInsurancePremium> GetInsurancePremiumByIdAsync(Guid premiumId, CancellationToken cancellationToken = default)
        {
            var premium = await DbContext.Set<EmployeeInsurancePremium>()
                .FirstOrDefaultAsync(p => p.Id == premiumId, cancellationToken);

            return premium;
        }

        public async Task AddInsurancePremiumAsync(EmployeeInsurancePremium premium, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeInsurancePremium>().AddAsync(premium, cancellationToken);
        }

        public void RemoveInsurancePremium(EmployeeInsurancePremium premium)
        {
            DbContext.Set<EmployeeInsurancePremium>().Remove(premium);
        }

        public async Task<IReadOnlyList<EmployeeLoan>> GetLoansByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var loans = await DbContext.Set<EmployeeLoan>()
                .Where(loan => loan.EmployeeId == employeeId)
                .OrderByDescending(loan => loan.RequestedDate)
                .ToListAsync(cancellationToken);

            return loans;
        }

        public async Task<EmployeeLoan> GetLoanByIdAsync(Guid loanId, CancellationToken cancellationToken = default)
        {
            var loan = await DbContext.Set<EmployeeLoan>()
                .FirstOrDefaultAsync(l => l.Id == loanId, cancellationToken);

            return loan;
        }

        public async Task AddLoanAsync(EmployeeLoan loan, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeLoan>().AddAsync(loan, cancellationToken);
        }

        public async Task<IReadOnlyList<Employee>> GetPayrollEligibleEmployeesAsync(CancellationToken cancellationToken = default)
        {
            var payableStatuses = new[] { EmploymentStatus.Active, EmploymentStatus.OnLeave };

            var employees = await DbSet
                .Include(e => e.Salaries)
                    .ThenInclude(salary => salary.Components)
                .Include(e => e.Salaries)
                    .ThenInclude(salary => salary.Deductions)
                .Include(e => e.Salaries)
                    .ThenInclude(salary => salary.InsurancePremiums)
                .Where(e => payableStatuses.Contains(e.EmploymentStatus) && e.Salaries.Any())
                .ToListAsync(cancellationToken);

            return employees;
        }

        public async Task<IReadOnlyList<EmployeeLoan>> GetApprovedLoansByEmployeeIdsAsync(IReadOnlyList<Guid> employeeIds, CancellationToken cancellationToken = default)
        {
            var loans = await DbContext.Set<EmployeeLoan>()
                .Where(loan => loan.Status == LoanStatus.Approved && employeeIds.Contains(loan.EmployeeId))
                .ToListAsync(cancellationToken);

            return loans;
        }

        public async Task<IReadOnlyList<SalaryAdjustment>> GetSalaryAdjustmentsByFilterAsync(Guid? employeeId, Guid? fiscalYearId, int? monthIndex, AdjustmentStatus? status, CancellationToken cancellationToken = default)
        {
            IQueryable<SalaryAdjustment> adjustmentsQuery = DbContext.Set<SalaryAdjustment>();

            if (employeeId.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.EmployeeId == employeeId.Value);
            }

            if (fiscalYearId.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.FiscalYearId == fiscalYearId.Value);
            }

            if (monthIndex.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.MonthIndex == monthIndex.Value);
            }

            if (status.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.Status == status.Value);
            }

            var adjustments = await adjustmentsQuery
                .OrderBy(a => a.MonthIndex)
                .ThenBy(a => a.CreatedTs)
                .ToListAsync(cancellationToken);

            return adjustments;
        }

        public async Task<SalaryAdjustment> GetSalaryAdjustmentByIdAsync(Guid adjustmentId, CancellationToken cancellationToken = default)
        {
            var adjustment = await DbContext.Set<SalaryAdjustment>()
                .FirstOrDefaultAsync(a => a.Id == adjustmentId, cancellationToken);

            return adjustment;
        }

        public async Task<IReadOnlyList<SalaryAdjustment>> GetPendingSalaryAdjustmentsForPeriodAsync(Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default)
        {
            var adjustments = await DbContext.Set<SalaryAdjustment>()
                .Where(a => a.FiscalYearId == fiscalYearId
                    && a.MonthIndex == monthIndex
                    && a.Status == AdjustmentStatus.Pending)
                .ToListAsync(cancellationToken);

            return adjustments;
        }

        public async Task<IReadOnlyList<SalaryAdjustment>> GetSalaryAdjustmentsAppliedToSlipsAsync(IReadOnlyList<Guid> slipIds, CancellationToken cancellationToken = default)
        {
            var adjustments = await DbContext.Set<SalaryAdjustment>()
                .Where(a => a.AppliedSalarySlipId != null && slipIds.Contains(a.AppliedSalarySlipId.Value))
                .ToListAsync(cancellationToken);

            return adjustments;
        }

        public async Task AddSalaryAdjustmentAsync(SalaryAdjustment adjustment, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<SalaryAdjustment>().AddAsync(adjustment, cancellationToken);
        }

        public void RemoveSalaryAdjustment(SalaryAdjustment adjustment)
        {
            DbContext.Set<SalaryAdjustment>().Remove(adjustment);
        }

        // Qualifications and Documents (2026-07-23, moved here from TeacherRepository).

        public async Task<IReadOnlyList<EmployeeQualification>> GetQualificationsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var qualifications = await DbContext.Set<EmployeeQualification>()
                .Where(qualification => qualification.EmployeeId == employeeId)
                .OrderByDescending(qualification => qualification.CompletionYear)
                .ToListAsync(cancellationToken);

            return qualifications;
        }

        public async Task<EmployeeQualification> GetQualificationByIdAsync(Guid qualificationId, CancellationToken cancellationToken = default)
        {
            var qualification = await DbContext.Set<EmployeeQualification>()
                .FirstOrDefaultAsync(q => q.Id == qualificationId, cancellationToken);

            return qualification;
        }

        public async Task AddQualificationAsync(EmployeeQualification qualification, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeQualification>().AddAsync(qualification, cancellationToken);
        }

        public void RemoveQualification(EmployeeQualification qualification)
        {
            DbContext.Set<EmployeeQualification>().Remove(qualification);
        }

        public async Task<IReadOnlyList<EmployeeDocument>> GetDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var documents = await DbContext.Set<EmployeeDocument>()
                .Where(document => document.EmployeeId == employeeId)
                .OrderBy(document => document.DocumentTypeCode)
                .ThenBy(document => document.DocumentName)
                .ToListAsync(cancellationToken);

            return documents;
        }

        public async Task<EmployeeDocument> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await DbContext.Set<EmployeeDocument>()
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            return document;
        }

        public async Task AddDocumentAsync(EmployeeDocument document, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeDocument>().AddAsync(document, cancellationToken);
        }

        public void RemoveDocument(EmployeeDocument document)
        {
            DbContext.Set<EmployeeDocument>().Remove(document);
        }

        // Leave balances (2026-07-23).

        public async Task<IReadOnlyList<EmployeeLeaveBalance>> GetLeaveBalancesByEmployeeIdAsync(Guid employeeId, Guid fiscalYearId, CancellationToken cancellationToken = default)
        {
            var balances = await DbContext.Set<EmployeeLeaveBalance>()
                .Include(b => b.LeaveType)
                .Include(b => b.FiscalYear)
                .Where(b => b.EmployeeId == employeeId && b.FiscalYearId == fiscalYearId)
                .ToListAsync(cancellationToken);

            return balances;
        }

        public async Task<EmployeeLeaveBalance> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, Guid fiscalYearId, CancellationToken cancellationToken = default)
        {
            var balance = await DbContext.Set<EmployeeLeaveBalance>()
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.FiscalYearId == fiscalYearId, cancellationToken);

            return balance;
        }

        public async Task<EmployeeLeaveBalance> GetLeaveBalanceByIdAsync(Guid leaveBalanceId, CancellationToken cancellationToken = default)
        {
            var balance = await DbContext.Set<EmployeeLeaveBalance>()
                .FirstOrDefaultAsync(b => b.Id == leaveBalanceId, cancellationToken);

            return balance;
        }

        public async Task AddLeaveBalanceAsync(EmployeeLeaveBalance leaveBalance, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EmployeeLeaveBalance>().AddAsync(leaveBalance, cancellationToken);
        }

        // TeacherAssignment (2026-08-06, moved here from the removed TeacherRepository --
        // TeacherId FKs directly to this aggregate's Id now).

        public async Task<bool> HasAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var hasAssignments = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.TeacherId == teacherId, cancellationToken);

            return hasAssignments;
        }

        public async Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            // The year chain is included so the employee detail can build service history from
            // the same query the assignments tab uses.
            var assignments = await DbContext.Set<TeacherAssignment>()
                .Include(assignment => assignment.ClassSubject)
                    .ThenInclude(cs => cs.AcademicClass)
                        .ThenInclude(c => c.AcademicYear)
                .Include(assignment => assignment.ClassSection)
                .Include(assignment => assignment.TimePeriod)
                .Where(assignment => assignment.TeacherId == teacherId)
                .OrderBy(assignment => assignment.ClassSubject.AcademicClass.AcademicYear.StartDate)
                .ThenBy(assignment => assignment.ClassSubject.SubjectCode)
                .ToListAsync(cancellationToken);

            return assignments;
        }

        public async Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByClassSubjectIdsAsync(IReadOnlyCollection<Guid> classSubjectIds, CancellationToken cancellationToken = default)
        {
            // Who teaches these subjects -- one batched query for the student profile's
            // subjects-studying block, the assigned Employee included for the name.
            var assignments = await DbContext.Set<TeacherAssignment>()
                .Include(assignment => assignment.Employee)
                .Where(assignment => classSubjectIds.Contains(assignment.ClassSubjectId))
                .ToListAsync(cancellationToken);

            return assignments;
        }

        public async Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByAcademicClassAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            IQueryable<TeacherAssignment> assignmentsQuery = DbContext.Set<TeacherAssignment>()
                .Include(assignment => assignment.Employee)
                .Include(assignment => assignment.ClassSubject)
                .Include(assignment => assignment.ClassSection)
                .Include(assignment => assignment.TimePeriod)
                .Where(assignment => assignment.ClassSubject.AcademicClassId == academicClassId);

            if (classSectionId.HasValue)
            {
                assignmentsQuery = assignmentsQuery.Where(assignment => assignment.ClassSectionId == classSectionId.Value);
            }

            var assignments = await assignmentsQuery
                .OrderBy(assignment => assignment.ClassSubject.SubjectCode)
                .ThenBy(assignment => assignment.Employee.FirstName)
                .ToListAsync(cancellationToken);

            return assignments;
        }

        public async Task<TeacherAssignment> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            var assignment = await DbContext.Set<TeacherAssignment>()
                .Include(a => a.ClassSubject)
                .Include(a => a.ClassSection)
                .Include(a => a.TimePeriod)
                .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);

            return assignment;
        }

        public async Task<bool> AssignmentExistsAsync(Guid teacherId, Guid classSubjectId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            // Exact triple match, null section included -- the unique index can't catch the
            // duplicate-null case (Postgres treats NULLs as distinct), so this check must.
            var assignmentExists = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.TeacherId == teacherId
                    && assignment.ClassSubjectId == classSubjectId
                    && assignment.ClassSectionId == classSectionId, cancellationToken);

            return assignmentExists;
        }

        public async Task<bool> ClassTeacherExistsForSectionAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            // "At most one class teacher per ClassSection" -- regardless of which subject the
            // class-teacher assignment rides on.
            var classTeacherExists = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.IsClassTeacher
                    && assignment.ClassSectionId == classSectionId, cancellationToken);

            return classTeacherExists;
        }

        public async Task<bool> TeacherHasTimePeriodConflictAsync(Guid teacherId, Guid timePeriodId, CancellationToken cancellationToken = default)
        {
            // TimePeriod.StartTime/EndTime is a fixed, class-independent definition -- only which
            // periods apply to which class varies, via ClassTimePeriod -- so two assignments for
            // the same employee sharing the same TimePeriod row necessarily share the same
            // wall-clock slot, regardless of which class/subject/section either one is for.
            var hasConflict = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.TeacherId == teacherId
                    && assignment.TimePeriodId == timePeriodId, cancellationToken);

            return hasConflict;
        }

        public async Task<bool> TeacherHasTimePeriodConflictAsync(Guid teacherId, Guid timePeriodId, Guid excludeAssignmentId, CancellationToken cancellationToken = default)
        {
            var hasConflict = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.TeacherId == teacherId
                    && assignment.TimePeriodId == timePeriodId
                    && assignment.Id != excludeAssignmentId, cancellationToken);

            return hasConflict;
        }

        public async Task AddAssignmentAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<TeacherAssignment>().AddAsync(assignment, cancellationToken);
        }

        public void RemoveAssignment(TeacherAssignment assignment)
        {
            DbContext.Set<TeacherAssignment>().Remove(assignment);
        }
    }
}
