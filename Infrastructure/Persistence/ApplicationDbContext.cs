using System.Reflection;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Entities.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid,
        ApplicationUserClaim,
        ApplicationUserRole,
        ApplicationUserLogin,
        ApplicationRoleClaim,
        ApplicationUserToken>
    {
        private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<Menu> Menus => Set<Menu>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<ConfigType> ConfigTypes => Set<ConfigType>();

        public DbSet<Config> Configs => Set<Config>();

        public DbSet<SystemAccessLog> SystemAccessLogs => Set<SystemAccessLog>();

        public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

        public DbSet<AppConfig> AppConfigs => Set<AppConfig>();

        public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();

        public DbSet<AcademicClass> AcademicClasses => Set<AcademicClass>();

        public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();

        public DbSet<Employee> Employees => Set<Employee>();

        public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();

        public DbSet<Teacher> Teachers => Set<Teacher>();

        public DbSet<TeacherQualification> TeacherQualifications => Set<TeacherQualification>();

        public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();

        public DbSet<Guardian> Guardians => Set<Guardian>();

        public DbSet<Student> Students => Set<Student>();

        public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();

        public DbSet<Enrollment> Enrollments => Set<Enrollment>();

        public DbSet<EnrollmentSubject> EnrollmentSubjects => Set<EnrollmentSubject>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampAuditableEntities();

            return base.SaveChangesAsync(cancellationToken);
        }

        private void StampAuditableEntities()
        {
            var currentUserName = _currentUserService.UserName ?? "system";
            var currentTimestamp = DateTimeOffset.UtcNow;

            var auditableEntries = ChangeTracker.Entries<IAuditableEntity>();
            foreach (var entry in auditableEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = currentUserName;
                    entry.Entity.CreatedTs = currentTimestamp;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedBy = currentUserName;
                    entry.Entity.UpdatedTs = currentTimestamp;
                }
            }

            var softDeleteEntries = ChangeTracker.Entries<ISoftDeleteAuditableEntity>();
            foreach (var entry in softDeleteEntries)
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedBy = currentUserName;
                    entry.Entity.DeletedTs = currentTimestamp;
                }
            }
        }
    }
}
