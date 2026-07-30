using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Dashboard;
using Application.Dashboard.Dtos;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardService(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, ICurrentUserService currentUserService, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public async Task<CommonResponse<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            // Soft-deleted users are excluded automatically by the global !IsDeleted query filter.
            var totalUserCount = await _dbContext.Users.CountAsync(cancellationToken);
            var activeUserCount = await _dbContext.Users.CountAsync(user => user.IsActive, cancellationToken);
            var distinctErrorCount = await _unitOfWork.ErrorLogs.GetDistinctErrorCountAsync(cancellationToken);
            var totalErrorCount = await _unitOfWork.ErrorLogs.GetTotalOccurrenceCountAsync(cancellationToken);

            var dashboardSummaryDto = new DashboardSummaryDto
            {
                TotalUserCount = totalUserCount,
                ActiveUserCount = activeUserCount,
                DistinctErrorCount = distinctErrorCount,
                TotalErrorCount = totalErrorCount
            };

            var successResponse = CommonResponse<DashboardSummaryDto>.Success(dashboardSummaryDto);
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentStatsDto>> GetEnrollmentStatsAsync(CancellationToken cancellationToken = default)
        {
            var totalStudents = await _dbContext.Students.CountAsync(cancellationToken);
            var totalActiveEnrollments = await _dbContext.Enrollments.CountAsync(enrollment => enrollment.Status == EnrollmentStatus.Enrolled, cancellationToken);

            var statusGroups = await _dbContext.Enrollments
                .GroupBy(enrollment => enrollment.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            var enrollmentsByStatus = new List<EnrollmentStatusCountDto>();
            foreach (var statusGroup in statusGroups)
            {
                var enrollmentStatusCountDto = new EnrollmentStatusCountDto
                {
                    Status = statusGroup.Status,
                    Count = statusGroup.Count
                };
                enrollmentsByStatus.Add(enrollmentStatusCountDto);
            }

            var enrollmentsByGrade = await GetActiveEnrollmentsByGradeAsync(cancellationToken);

            var enrollmentStatsDto = new EnrollmentStatsDto
            {
                TotalStudents = totalStudents,
                TotalActiveEnrollments = totalActiveEnrollments,
                EnrollmentsByStatus = enrollmentsByStatus,
                EnrollmentsByGrade = enrollmentsByGrade
            };

            var successResponse = CommonResponse<EnrollmentStatsDto>.Success(enrollmentStatsDto);
            return successResponse;
        }

        public async Task<CommonResponse<TeacherListWidgetDto>> GetTeacherListWidgetAsync(int take, CancellationToken cancellationToken = default)
        {
            var totalTeachers = await _dbContext.Teachers.CountAsync(cancellationToken);
            var activeTeachers = await _dbContext.Teachers.CountAsync(teacher => teacher.Employee.EmploymentStatus == EmploymentStatus.Active, cancellationToken);

            var recentTeacherEntities = await _dbContext.Teachers
                .Include(teacher => teacher.Employee)
                .OrderByDescending(teacher => teacher.Employee.CreatedTs)
                .Take(take)
                .ToListAsync(cancellationToken);

            var recentTeachers = new List<DashboardTeacherSummaryDto>();
            foreach (var teacher in recentTeacherEntities)
            {
                var dashboardTeacherSummaryDto = new DashboardTeacherSummaryDto
                {
                    Id = teacher.Id,
                    EmployeeNo = teacher.Employee.EmployeeCode,
                    FirstName = teacher.Employee.FirstName,
                    MiddleName = teacher.Employee.MiddleName,
                    LastName = teacher.Employee.LastName,
                    Status = teacher.Employee.EmploymentStatus,
                    JoiningDate = teacher.Employee.JoinDate
                };
                recentTeachers.Add(dashboardTeacherSummaryDto);
            }

            var teacherListWidgetDto = new TeacherListWidgetDto
            {
                TotalTeachers = totalTeachers,
                ActiveTeachers = activeTeachers,
                RecentTeachers = recentTeachers
            };

            var successResponse = CommonResponse<TeacherListWidgetDto>.Success(teacherListWidgetDto);
            return successResponse;
        }

        public async Task<CommonResponse<UserListWidgetDto>> GetUserListWidgetAsync(int take, CancellationToken cancellationToken = default)
        {
            var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
            var activeUsers = await _dbContext.Users.CountAsync(user => user.IsActive, cancellationToken);

            var recentUserEntities = await _dbContext.Users
                .OrderByDescending(user => user.CreatedTs)
                .Take(take)
                .ToListAsync(cancellationToken);

            var recentUsers = new List<DashboardUserSummaryDto>();
            foreach (var user in recentUserEntities)
            {
                var dashboardUserSummaryDto = new DashboardUserSummaryDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserType = user.UserType,
                    IsActive = user.IsActive,
                    CreatedTs = user.CreatedTs
                };
                recentUsers.Add(dashboardUserSummaryDto);
            }

            var userListWidgetDto = new UserListWidgetDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                RecentUsers = recentUsers
            };

            var successResponse = CommonResponse<UserListWidgetDto>.Success(userListWidgetDto);
            return successResponse;
        }

        public async Task<CommonResponse<BarGraphDto>> GetBarGraphAsync(string metric, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(metric) || !DashboardBarGraphMetrics.All.Contains(metric))
            {
                var allowedMetrics = string.Join(", ", DashboardBarGraphMetrics.All);
                var validationFailureResponse = CommonResponse<BarGraphDto>.Fail(ResponseCodes.ValidationError, "metric must be one of: " + allowedMetrics + ".");
                return validationFailureResponse;
            }

            BarGraphDto barGraphDto;
            if (metric == DashboardBarGraphMetrics.EnrollmentsByGrade)
            {
                barGraphDto = await BuildEnrollmentsByGradeBarGraphAsync(cancellationToken);
            }
            else if (metric == DashboardBarGraphMetrics.EnrollmentsByMonth)
            {
                barGraphDto = await BuildEnrollmentsByMonthBarGraphAsync(cancellationToken);
            }
            else if (metric == DashboardBarGraphMetrics.StudentsByStatus)
            {
                barGraphDto = await BuildStudentsByStatusBarGraphAsync(cancellationToken);
            }
            else
            {
                barGraphDto = await BuildTeachersByStatusBarGraphAsync(cancellationToken);
            }

            var successResponse = CommonResponse<BarGraphDto>.Success(barGraphDto);
            return successResponse;
        }

        public async Task<CommonResponse<CurrentAcademicYearDto>> GetCurrentAcademicYearAsync(CancellationToken cancellationToken = default)
        {
            var currentAcademicYear = await GetCurrentAcademicYearEntityAsync(cancellationToken);
            if (currentAcademicYear == null)
            {
                var notFoundResponse = CommonResponse<CurrentAcademicYearDto>.Fail(ResponseCodes.NotFound, "No academic year is marked as current.");
                return notFoundResponse;
            }

            var totalClasses = await _dbContext.AcademicClasses.CountAsync(academicClass => academicClass.AcademicYearId == currentAcademicYear.Id, cancellationToken);

            var totalSections = await _dbContext.Set<ClassSection>()
                .CountAsync(classSection => classSection.AcademicClass.AcademicYearId == currentAcademicYear.Id, cancellationToken);

            var totalActiveEnrollments = await _dbContext.Enrollments
                .CountAsync(enrollment => enrollment.Status == EnrollmentStatus.Enrolled && enrollment.ClassSection.AcademicClass.AcademicYearId == currentAcademicYear.Id, cancellationToken);

            var currentAcademicYearDto = new CurrentAcademicYearDto
            {
                Id = currentAcademicYear.Id,
                Code = currentAcademicYear.Code,
                Name = currentAcademicYear.Name,
                StartDate = currentAcademicYear.StartDate,
                EndDate = currentAcademicYear.EndDate,
                TotalClasses = totalClasses,
                TotalSections = totalSections,
                TotalActiveEnrollments = totalActiveEnrollments
            };

            var successResponse = CommonResponse<CurrentAcademicYearDto>.Success(currentAcademicYearDto);
            return successResponse;
        }

        public async Task<CommonResponse<List<QuickMenuDto>>> GetQuickMenusAsync(int take, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                var unauthorizedResponse = CommonResponse<List<QuickMenuDto>>.Fail(ResponseCodes.Unauthorized, "No authenticated user was found.");
                return unauthorizedResponse;
            }

            var user = await _userManager.FindByIdAsync(currentUserId.Value.ToString());
            if (user == null)
            {
                var notFoundResponse = CommonResponse<List<QuickMenuDto>>.Fail(ResponseCodes.NotFound, "User was not found.");
                return notFoundResponse;
            }

            var roleNames = await _userManager.GetRolesAsync(user);
            var roleIds = await _dbContext.Roles
                .Where(role => roleNames.Contains(role.Name))
                .Select(role => role.Id)
                .ToListAsync(cancellationToken);

            var allowedMenuIds = await _dbContext.RoleClaims
                .Where(roleClaim => roleIds.Contains(roleClaim.RoleId))
                .Select(roleClaim => roleClaim.MenuId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Quick-menu suggestions are visible SUB_MENU rows explicitly curated via
            // Menu.IsQuickLink (2026-07-28) that the current user is actually allowed to open --
            // PERMISSION leaves, hidden rows, and any SUB_MENU not flagged as a quick link are
            // excluded even if granted. IsQuickLink is additive on top of the permission check,
            // never a substitute for it.
            var quickMenuEntities = await _dbContext.Menus
                .Where(menu => allowedMenuIds.Contains(menu.Id)
                    && menu.MenuType == MenuTypes.SubMenu
                    && !menu.IsHidden
                    && menu.IsQuickLink)
                .OrderBy(menu => menu.Order)
                .Take(take)
                .ToListAsync(cancellationToken);

            var quickMenuDtos = new List<QuickMenuDto>();
            foreach (var menu in quickMenuEntities)
            {
                var quickMenuDto = new QuickMenuDto
                {
                    Id = menu.Id,
                    Code = menu.Code,
                    DisplayName = menu.DisplayName,
                    Url = menu.Url,
                    Icon = menu.Icon,
                    Order = menu.Order
                };
                quickMenuDtos.Add(quickMenuDto);
            }

            var successResponse = CommonResponse<List<QuickMenuDto>>.Success(quickMenuDtos);
            return successResponse;
        }

        public async Task<CommonResponse<AccountsDashboardSummaryDto>> GetAccountsSummaryAsync(int take, CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var currentFiscalYear = await _unitOfWork.FiscalYears.GetCurrentYearAsync(cancellationToken);

            var payments = _dbContext.Set<FeePayment>().Where(payment => payment.Status != FeePaymentStatus.Voided);

            var feeCollectedToday = await payments
                .Where(payment => payment.PaymentDate.Date == today)
                .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

            var feeCollectedThisMonth = await payments
                .Where(payment => payment.PaymentDate.Date >= startOfMonth)
                .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

            var feeCollectedThisFiscalYear = 0m;
            if (currentFiscalYear != null)
            {
                feeCollectedThisFiscalYear = await payments
                    .Where(payment => payment.PaymentDate.Date >= currentFiscalYear.StartDate.Date && payment.PaymentDate.Date <= currentFiscalYear.EndDate.Date)
                    .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
            }

            // Same outstanding-balance formula used throughout FeeInvoiceService (e.g.
            // GetStatementAsync/SearchStudentsAsync): NetAmount - PaidAmount, excluding
            // Draft (not yet a real charge) and Cancelled (voided/carried-forward) invoices.
            var openInvoices = _dbContext.Set<FeeInvoice>()
                .Where(invoice => invoice.Status != FeeInvoiceStatus.Draft && invoice.Status != FeeInvoiceStatus.Cancelled);

            var totalOutstandingDue = await openInvoices
                .SumAsync(invoice => (decimal?)(invoice.NetAmount - invoice.PaidAmount), cancellationToken) ?? 0m;

            var statusGroups = await _dbContext.Set<FeeInvoice>()
                .GroupBy(invoice => invoice.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            var invoiceCountsByStatus = new List<FeeInvoiceStatusCountDto>();
            foreach (var statusGroup in statusGroups)
            {
                invoiceCountsByStatus.Add(new FeeInvoiceStatusCountDto { Status = statusGroup.Status, Count = statusGroup.Count });
            }

            var pendingFeeAdjustmentCount = await _dbContext.Set<FeeAdjustment>()
                .CountAsync(adjustment => adjustment.Status == AdjustmentStatus.Pending, cancellationToken);

            var latestPayrollRun = await _dbContext.Set<PayrollRun>()
                .Include(run => run.FiscalYear)
                .Include(run => run.Slips)
                .OrderByDescending(run => run.CreatedTs)
                .FirstOrDefaultAsync(cancellationToken);

            CurrentPayrollRunSummaryDto currentPayrollRunDto = null;
            if (latestPayrollRun != null)
            {
                var liveSlips = latestPayrollRun.Slips.Where(slip => slip.Status != SalarySlipStatus.Cancelled).ToList();
                currentPayrollRunDto = new CurrentPayrollRunSummaryDto
                {
                    PayrollRunId = latestPayrollRun.Id,
                    FiscalYearCode = latestPayrollRun.FiscalYear != null ? latestPayrollRun.FiscalYear.Code : null,
                    MonthIndex = latestPayrollRun.MonthIndex,
                    Status = latestPayrollRun.Status,
                    SlipCount = liveSlips.Count,
                    TotalNetPay = liveSlips.Sum(slip => slip.NetPay)
                };
            }

            var recentPaymentEntities = await _dbContext.Set<FeePayment>()
                .Include(payment => payment.Enrollment)
                    .ThenInclude(enrollment => enrollment.Student)
                .OrderByDescending(payment => payment.PaymentDate)
                .Take(take)
                .ToListAsync(cancellationToken);

            var recentPayments = new List<RecentFeePaymentDto>();
            foreach (var payment in recentPaymentEntities)
            {
                var student = payment.Enrollment != null ? payment.Enrollment.Student : null;
                recentPayments.Add(new RecentFeePaymentDto
                {
                    Id = payment.Id,
                    ReceiptNo = payment.ReceiptNo,
                    StudentName = student != null ? BuildFullName(student.FirstName, student.MiddleName, student.LastName) : null,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate
                });
            }

            var accountsDashboardSummaryDto = new AccountsDashboardSummaryDto
            {
                FeeCollectedToday = feeCollectedToday,
                FeeCollectedThisMonth = feeCollectedThisMonth,
                FeeCollectedThisFiscalYear = feeCollectedThisFiscalYear,
                TotalOutstandingDue = totalOutstandingDue,
                InvoiceCountsByStatus = invoiceCountsByStatus,
                PendingFeeAdjustmentCount = pendingFeeAdjustmentCount,
                CurrentPayrollRun = currentPayrollRunDto,
                RecentPayments = recentPayments
            };

            var successResponse = CommonResponse<AccountsDashboardSummaryDto>.Success(accountsDashboardSummaryDto);
            return successResponse;
        }

        public async Task<CommonResponse<HrDashboardSummaryDto>> GetHrSummaryAsync(int take, CancellationToken cancellationToken = default)
        {
            var totalEmployees = await _dbContext.Employees.CountAsync(cancellationToken);

            var statusGroups = await _dbContext.Employees
                .GroupBy(employee => employee.EmploymentStatus)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            var employeesByStatus = new List<EmploymentStatusCountDto>();
            foreach (var statusGroup in statusGroups)
            {
                employeesByStatus.Add(new EmploymentStatusCountDto { Status = statusGroup.Status, Count = statusGroup.Count });
            }

            var categoryGroups = await _dbContext.Employees
                .GroupBy(employee => employee.EmployeeCategoryCode)
                .Select(group => new { CategoryCode = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            var categoryOptions = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.EmployeeCategory, cancellationToken);
            var categoryLabels = ConfigLabelHelper.BuildLabelMap(categoryOptions);

            var employeesByCategory = new List<EmployeeCategoryCountDto>();
            foreach (var categoryGroup in categoryGroups)
            {
                employeesByCategory.Add(new EmployeeCategoryCountDto
                {
                    CategoryCode = categoryGroup.CategoryCode,
                    CategoryLabel = ConfigLabelHelper.Resolve(categoryLabels, categoryGroup.CategoryCode),
                    Count = categoryGroup.Count
                });
            }

            // HrStatus is the authoritative gate (LeaveRequest's own design -- HR can decide
            // regardless of ManagerStatus), so this is the real "needs HR attention" count.
            var pendingLeaveRequestCount = await _dbContext.Set<LeaveRequest>()
                .CountAsync(request => request.HrStatus == LeaveApprovalStatus.Pending, cancellationToken);

            var pendingLoanRequestCount = await _dbContext.Set<EmployeeLoan>()
                .CountAsync(loan => loan.Status == LoanStatus.PendingApproval, cancellationToken);

            var recentHireEntities = await _dbContext.Employees
                .Where(employee => employee.JoinDate != null)
                .OrderByDescending(employee => employee.JoinDate)
                .Take(take)
                .ToListAsync(cancellationToken);

            var jobPositionOptions = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.JobPosition, cancellationToken);
            var jobPositionLabels = ConfigLabelHelper.BuildLabelMap(jobPositionOptions);

            var recentHires = new List<RecentHireDto>();
            foreach (var employee in recentHireEntities)
            {
                recentHires.Add(new RecentHireDto
                {
                    EmployeeId = employee.Id,
                    FullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName),
                    JobPositionCode = employee.JobPositionCode,
                    JobPositionLabel = ConfigLabelHelper.Resolve(jobPositionLabels, employee.JobPositionCode),
                    JoinDate = employee.JoinDate.Value
                });
            }

            var today = DateTime.UtcNow.Date;
            var horizon = today.AddDays(30);

            var activeEmployeesWithDates = await _dbContext.Employees
                .Where(employee => employee.EmploymentStatus == EmploymentStatus.Active
                    && (employee.DateOfBirth != null || employee.JoinDate != null))
                .ToListAsync(cancellationToken);

            var upcomingBirthdays = new List<UpcomingHrEventDto>();
            var upcomingWorkAnniversaries = new List<UpcomingHrEventDto>();
            foreach (var employee in activeEmployeesWithDates)
            {
                var fullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName);

                if (employee.DateOfBirth.HasValue)
                {
                    var nextBirthday = RecurringDateHelper.ResolveNextOccurrence(employee.DateOfBirth.Value, today);
                    if (nextBirthday <= horizon)
                    {
                        upcomingBirthdays.Add(new UpcomingHrEventDto { EmployeeId = employee.Id, FullName = fullName, Date = nextBirthday });
                    }
                }

                if (employee.JoinDate.HasValue)
                {
                    var nextAnniversary = RecurringDateHelper.ResolveNextOccurrence(employee.JoinDate.Value, today);
                    if (nextAnniversary <= horizon && nextAnniversary.Year > employee.JoinDate.Value.Year)
                    {
                        upcomingWorkAnniversaries.Add(new UpcomingHrEventDto { EmployeeId = employee.Id, FullName = fullName, Date = nextAnniversary });
                    }
                }
            }

            upcomingBirthdays = upcomingBirthdays.OrderBy(item => item.Date).ToList();
            upcomingWorkAnniversaries = upcomingWorkAnniversaries.OrderBy(item => item.Date).ToList();

            var hrDashboardSummaryDto = new HrDashboardSummaryDto
            {
                TotalEmployees = totalEmployees,
                EmployeesByStatus = employeesByStatus,
                EmployeesByCategory = employeesByCategory,
                PendingLeaveRequestCount = pendingLeaveRequestCount,
                PendingLoanRequestCount = pendingLoanRequestCount,
                RecentHires = recentHires,
                UpcomingBirthdays = upcomingBirthdays,
                UpcomingWorkAnniversaries = upcomingWorkAnniversaries
            };

            var successResponse = CommonResponse<HrDashboardSummaryDto>.Success(hrDashboardSummaryDto);
            return successResponse;
        }

        // Small standalone helper, same "not shared with EmployeeMapper.BuildFullName" call as
        // that mapper's own doc comment already makes for its own duplicate -- Dashboard reads
        // across every aggregate and shouldn't pull in per-feature mappers for one string.
        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                parts.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                parts.Add(middleName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                parts.Add(lastName);
            }

            return string.Join(" ", parts);
        }

        private async Task<AcademicYear> GetCurrentAcademicYearEntityAsync(CancellationToken cancellationToken)
        {
            var currentAcademicYears = await _unitOfWork.AcademicYears.GetCurrentYearsAsync(cancellationToken);
            var currentAcademicYear = currentAcademicYears.FirstOrDefault();
            return currentAcademicYear;
        }

        private async Task<List<GradeEnrollmentCountDto>> GetActiveEnrollmentsByGradeAsync(CancellationToken cancellationToken)
        {
            var gradeEnrollmentCountDtos = new List<GradeEnrollmentCountDto>();

            var currentAcademicYear = await GetCurrentAcademicYearEntityAsync(cancellationToken);
            if (currentAcademicYear == null)
            {
                return gradeEnrollmentCountDtos;
            }

            var activeEnrollments = await _dbContext.Enrollments
                .Include(enrollment => enrollment.ClassSection)
                    .ThenInclude(classSection => classSection.AcademicClass)
                .Where(enrollment => enrollment.Status == EnrollmentStatus.Enrolled && enrollment.ClassSection.AcademicClass.AcademicYearId == currentAcademicYear.Id)
                .ToListAsync(cancellationToken);

            var countsByGradeCode = new Dictionary<string, int>();
            foreach (var enrollment in activeEnrollments)
            {
                var gradeCode = enrollment.ClassSection.AcademicClass.GradeCode;
                if (countsByGradeCode.ContainsKey(gradeCode))
                {
                    countsByGradeCode[gradeCode] = countsByGradeCode[gradeCode] + 1;
                }
                else
                {
                    countsByGradeCode[gradeCode] = 1;
                }
            }

            var gradeConfigs = await _dbContext.Configs
                .Where(config => config.TypeCode == ConfigTypeCodes.Grade)
                .OrderBy(config => config.Order)
                .ToListAsync(cancellationToken);

            foreach (var gradeConfig in gradeConfigs)
            {
                var count = 0;
                if (countsByGradeCode.ContainsKey(gradeConfig.Code))
                {
                    count = countsByGradeCode[gradeConfig.Code];
                }

                var gradeEnrollmentCountDto = new GradeEnrollmentCountDto
                {
                    GradeCode = gradeConfig.Code,
                    GradeLabel = gradeConfig.Label,
                    Count = count
                };
                gradeEnrollmentCountDtos.Add(gradeEnrollmentCountDto);
            }

            return gradeEnrollmentCountDtos;
        }

        private async Task<BarGraphDto> BuildEnrollmentsByGradeBarGraphAsync(CancellationToken cancellationToken)
        {
            var gradeCounts = await GetActiveEnrollmentsByGradeAsync(cancellationToken);

            var labels = new List<string>();
            var data = new List<int>();
            foreach (var gradeCount in gradeCounts)
            {
                labels.Add(gradeCount.GradeLabel);
                data.Add(gradeCount.Count);
            }

            var series = new BarGraphSeriesDto { Name = "Active Enrollments", Data = data };
            var barGraphDto = new BarGraphDto
            {
                Metric = DashboardBarGraphMetrics.EnrollmentsByGrade,
                Title = "Active Enrollments by Grade",
                Labels = labels,
                Series = new List<BarGraphSeriesDto> { series }
            };
            return barGraphDto;
        }

        private async Task<BarGraphDto> BuildEnrollmentsByMonthBarGraphAsync(CancellationToken cancellationToken)
        {
            const int monthCount = 6;
            var today = DateTime.UtcNow.Date;
            var startMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-(monthCount - 1));

            var enrollmentDates = await _dbContext.Enrollments
                .Where(enrollment => enrollment.EnrollmentDate != null && enrollment.EnrollmentDate >= startMonth)
                .Select(enrollment => enrollment.EnrollmentDate.Value)
                .ToListAsync(cancellationToken);

            var countsByMonthKey = new Dictionary<string, int>();
            foreach (var enrollmentDate in enrollmentDates)
            {
                var monthKey = enrollmentDate.ToString("yyyy-MM");
                if (countsByMonthKey.ContainsKey(monthKey))
                {
                    countsByMonthKey[monthKey] = countsByMonthKey[monthKey] + 1;
                }
                else
                {
                    countsByMonthKey[monthKey] = 1;
                }
            }

            var labels = new List<string>();
            var data = new List<int>();
            for (var monthIndex = 0; monthIndex < monthCount; monthIndex++)
            {
                var monthDate = startMonth.AddMonths(monthIndex);
                var monthKey = monthDate.ToString("yyyy-MM");
                var count = 0;
                if (countsByMonthKey.ContainsKey(monthKey))
                {
                    count = countsByMonthKey[monthKey];
                }

                labels.Add(monthDate.ToString("MMM yyyy"));
                data.Add(count);
            }

            var series = new BarGraphSeriesDto { Name = "New Enrollments", Data = data };
            var barGraphDto = new BarGraphDto
            {
                Metric = DashboardBarGraphMetrics.EnrollmentsByMonth,
                Title = "New Enrollments (Last 6 Months)",
                Labels = labels,
                Series = new List<BarGraphSeriesDto> { series }
            };
            return barGraphDto;
        }

        private async Task<BarGraphDto> BuildStudentsByStatusBarGraphAsync(CancellationToken cancellationToken)
        {
            var activeCount = await _dbContext.Students.CountAsync(student => student.Status == RecordStatus.Active, cancellationToken);
            var inactiveCount = await _dbContext.Students.CountAsync(student => student.Status == RecordStatus.Inactive, cancellationToken);

            var series = new BarGraphSeriesDto { Name = "Students", Data = new List<int> { activeCount, inactiveCount } };
            var barGraphDto = new BarGraphDto
            {
                Metric = DashboardBarGraphMetrics.StudentsByStatus,
                Title = "Students by Status",
                Labels = new List<string> { "Active", "Inactive" },
                Series = new List<BarGraphSeriesDto> { series }
            };
            return barGraphDto;
        }

        private async Task<BarGraphDto> BuildTeachersByStatusBarGraphAsync(CancellationToken cancellationToken)
        {
            // EmploymentStatus has more than two values (OnLeave/Suspended/Resigned/Terminated/
            // Retired) -- this graph keeps the same two-bucket shape as the student one by
            // treating anything other than Active as "inactive".
            var activeCount = await _dbContext.Teachers.CountAsync(teacher => teacher.Employee.EmploymentStatus == EmploymentStatus.Active, cancellationToken);
            var inactiveCount = await _dbContext.Teachers.CountAsync(teacher => teacher.Employee.EmploymentStatus != EmploymentStatus.Active, cancellationToken);

            var series = new BarGraphSeriesDto { Name = "Teachers", Data = new List<int> { activeCount, inactiveCount } };
            var barGraphDto = new BarGraphDto
            {
                Metric = DashboardBarGraphMetrics.TeachersByStatus,
                Title = "Teachers by Status",
                Labels = new List<string> { "Active", "Inactive" },
                Series = new List<BarGraphSeriesDto> { series }
            };
            return barGraphDto;
        }
    }
}
