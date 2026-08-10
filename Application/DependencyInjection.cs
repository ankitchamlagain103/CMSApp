using System.Reflection;
using Application.AcademicClasses;
using Application.AcademicYears;
using Application.AccessLogs;
using Application.AppConfigs;
using Application.Calendars;
using Application.Configs;
using Application.Dashboard;
using Application.Dashboard.Widgets;
using Application.DocumentTemplates;
using Application.Employees;
using Application.Enrollments;
using Application.ErrorLogs;
using Application.Exams;
using Application.FeeGenerationRuns;
using Application.FeeInvoices;
using Application.FeePayments;
using Application.FeeRules;
using Application.Fees;
using Application.GradeScales;
using Application.Guardians;
using Application.LeaveTypes;
using Application.Meetings;
using Application.Menus;
using Application.Notifications;
using Application.Payroll.FiscalYears;
using Application.Payroll.SalaryCalculations;
using Application.PayrollRuns;
using Application.Promotions;
using Application.Students;
using Application.TimePeriods;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            RegisterMenuServices(services);
            RegisterConfigServices(services);
            RegisterLoggingServices(services);
            RegisterStudentManagementServices(services);
            RegisterCalendarServices(services);
            RegisterDashboardWidgetServices(services);

            return services;
        }

        // Generic dashboard widget registry (2026-08-07) -- IDashboardWidgetRegistryService only
        // depends on other Application contracts (IRoleService, IDashboardWidgetProvider), so it
        // and its providers register here rather than in Infrastructure, even though the
        // concrete IDashboardService/IEmployeeService/IRoleService implementations they call
        // through don't. Add one AddScoped<IDashboardWidgetProvider, ...> line per new widget.
        private static void RegisterDashboardWidgetServices(IServiceCollection services)
        {
            services.AddScoped<IDashboardWidgetRegistryService, DashboardWidgetRegistryService>();
            services.AddScoped<IDashboardWidgetProvider, DashboardSummaryWidgetProvider>();
            services.AddScoped<IDashboardWidgetProvider, AccountsSummaryWidgetProvider>();
            services.AddScoped<IDashboardWidgetProvider, HrSummaryWidgetProvider>();
            services.AddScoped<IDashboardWidgetProvider, MyDashboardWidgetProvider>();
        }

        private static void RegisterCalendarServices(IServiceCollection services)
        {
            services.AddScoped<IBsAdConversionService, BsAdConversionService>();
            services.AddScoped<ICalendarService, CalendarService>();
            services.AddScoped<IMeetingService, MeetingService>();
        }

        private static void RegisterStudentManagementServices(IServiceCollection services)
        {
            services.AddScoped<IAcademicYearService, AcademicYearService>();
            services.AddScoped<IAcademicClassService, AcademicClassService>();
            services.AddScoped<IGuardianService, GuardianService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IGradeScaleService, GradeScaleService>();
            services.AddScoped<ITimePeriodService, TimePeriodService>();
            services.AddScoped<IPromotionService, PromotionService>();
            services.AddScoped<IFeeStructureService, FeeStructureService>();
            services.AddScoped<IFeeRuleService, FeeRuleService>();
            services.AddScoped<IFeeInvoiceService, FeeInvoiceService>();
            services.AddScoped<IFeeGenerationRunService, FeeGenerationRunService>();
            services.AddScoped<IFeePaymentService, FeePaymentService>();
            services.AddScoped<IFiscalYearService, FiscalYearService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IPayrollRunService, PayrollRunService>();
            services.AddScoped<ISalaryCalculatorService, SalaryCalculatorService>();
            services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
            services.AddScoped<ILeaveTypeService, LeaveTypeService>();
            services.AddScoped<INotificationService, NotificationService>();
        }

        private static void RegisterMenuServices(IServiceCollection services)
        {
            services.AddScoped<IMenuService, MenuService>();
        }

        private static void RegisterConfigServices(IServiceCollection services)
        {
            services.AddScoped<IConfigService, ConfigService>();
            services.AddScoped<IAppConfigService, AppConfigService>();
        }

        private static void RegisterLoggingServices(IServiceCollection services)
        {
            services.AddScoped<ISystemAccessLogService, SystemAccessLogService>();
            services.AddScoped<IErrorLogService, ErrorLogService>();
        }
    }
}
