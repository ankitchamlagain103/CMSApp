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

        public async Task<Employee> GetByIdWithTeacherAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var employee = await DbSet
                .Include(e => e.Teacher)
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
    }
}
