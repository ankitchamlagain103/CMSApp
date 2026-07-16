using Domain.Interfaces;

namespace Application.Common.Interfaces
{
    public interface IUnitOfWork
    {
        IMenuRepository Menus { get; }

        IConfigTypeRepository ConfigTypes { get; }

        IConfigRepository Configs { get; }

        ISystemAccessLogRepository SystemAccessLogs { get; }

        IErrorLogRepository ErrorLogs { get; }

        IAppConfigRepository AppConfigs { get; }

        IAcademicYearRepository AcademicYears { get; }

        IAcademicClassRepository AcademicClasses { get; }

        ITeacherRepository Teachers { get; }

        IGuardianRepository Guardians { get; }

        IStudentRepository Students { get; }

        IEnrollmentRepository Enrollments { get; }

        IFeeStructureRepository FeeStructures { get; }

        IFiscalYearRepository FiscalYears { get; }

        IEmployeeRepository Employees { get; }

        IDocumentTemplateRepository DocumentTemplates { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
