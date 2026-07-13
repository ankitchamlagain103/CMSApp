using System.Reflection;
using Application.AcademicClasses;
using Application.AcademicYears;
using Application.AccessLogs;
using Application.AppConfigs;
using Application.Configs;
using Application.Enrollments;
using Application.ErrorLogs;
using Application.Guardians;
using Application.Menus;
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

            return services;
        }

        private static void RegisterStudentManagementServices(IServiceCollection services)
        {
            services.AddScoped<IAcademicYearService, AcademicYearService>();
            services.AddScoped<IAcademicClassService, AcademicClassService>();
            services.AddScoped<ITeacherService, TeacherService>();
            services.AddScoped<IGuardianService, GuardianService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
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
