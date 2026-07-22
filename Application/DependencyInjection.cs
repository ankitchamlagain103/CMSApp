using System.Reflection;
using Application.AcademicClasses;
using Application.AcademicYears;
using Application.AccessLogs;
using Application.AppConfigs;
using Application.Calendars;
using Application.Configs;
using Application.DocumentTemplates;
using Application.Employees;
using Application.Enrollments;
using Application.ErrorLogs;
using Application.FeeGenerationRuns;
using Application.FeeInvoices;
using Application.FeePayments;
using Application.FeeRules;
using Application.Fees;
using Application.Guardians;
using Application.Meetings;
using Application.Menus;
using Application.Payroll.FiscalYears;
using Application.Payroll.SalaryCalculations;
using Application.PayrollRuns;
using Application.Students;
using Application.Teachers;
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

            return services;
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
            services.AddScoped<ITeacherService, TeacherService>();
            services.AddScoped<IGuardianService, GuardianService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
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
