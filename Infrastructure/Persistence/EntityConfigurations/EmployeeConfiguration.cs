using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeConfiguration : SoftDeleteAuditableEntityConfiguration<Employee>
    {
        public override void Configure(EntityTypeBuilder<Employee> builder)
        {
            base.Configure(builder);

            builder.ToTable("employees", "dbo");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                    .HasColumnName("id");

            builder.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .IsRequired(false);

            builder.Property(e => e.EmployeeCode)
                    .HasColumnName("employee_code")
                    .IsRequired()
                    .HasMaxLength(30);

            builder.Property(e => e.FirstName)
                    .HasColumnName("first_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(e => e.MiddleName)
                    .HasColumnName("middle_name")
                    .HasMaxLength(100);

            builder.Property(e => e.LastName)
                    .HasColumnName("last_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(e => e.Gender)
                    .HasColumnName("gender")
                    .IsRequired();

            builder.Property(e => e.DateOfBirth)
                    .HasColumnName("date_of_birth")
                    .HasColumnType("date");

            builder.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(255);

            builder.Property(e => e.Phone)
                    .HasColumnName("phone")
                    .HasMaxLength(20);

            builder.Property(e => e.JoinDate)
                    .HasColumnName("join_date")
                    .HasColumnType("date");

            // Config codes (TypeCodes ConfigTypeCodes.EmployeeCategory/JobPosition), not database
            // FKs -- same convention as GradeCode/SubjectCode.
            builder.Property(e => e.EmployeeCategoryCode)
                    .HasColumnName("employee_category_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(e => e.JobPositionCode)
                    .HasColumnName("job_position_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(e => e.EmploymentStatus)
                    .HasColumnName("employment_status")
                    .IsRequired();

            builder.Property(e => e.BankName)
                    .HasColumnName("bank_name")
                    .HasMaxLength(150);

            builder.Property(e => e.BankAccountNumber)
                    .HasColumnName("bank_account_number")
                    .HasMaxLength(50);

            builder.Property(e => e.PaymentMode)
                    .HasColumnName("payment_mode")
                    .IsRequired();

            // "Accounts and Codes" (2026-07-23) -- free-form, all optional, no format enforced
            // (see the doc comment on Employee.PanNumber for why).
            builder.Property(e => e.PanNumber)
                    .HasColumnName("pan_number")
                    .HasMaxLength(50);

            builder.Property(e => e.ProvidentFundNumber)
                    .HasColumnName("provident_fund_number")
                    .HasMaxLength(50);

            builder.Property(e => e.SsfNumber)
                    .HasColumnName("ssf_number")
                    .HasMaxLength(50);

            builder.Property(e => e.CitNumber)
                    .HasColumnName("cit_number")
                    .HasMaxLength(50);

            builder.Property(e => e.GratuityNumber)
                    .HasColumnName("gratuity_number")
                    .HasMaxLength(50);

            // Org fields (2026-07-23) -- Config codes (ConfigTypeCodes.Branch/Province/
            // EmployeeLevel), not database FKs, same convention as EmployeeCategoryCode above.
            builder.Property(e => e.BranchCode)
                    .HasColumnName("branch_code")
                    .HasMaxLength(100);

            builder.Property(e => e.ProvinceCode)
                    .HasColumnName("province_code")
                    .HasMaxLength(100);

            builder.Property(e => e.LevelCode)
                    .HasColumnName("level_code")
                    .HasMaxLength(100);

            builder.Property(e => e.ManagerId)
                    .HasColumnName("manager_id");

            // Address chain (2026-07-24) -- Config codes (ConfigTypeCodes.District/LocalLevel),
            // not database FKs, same convention as BranchCode/ProvinceCode/LevelCode above.
            builder.Property(e => e.DistrictCode)
                    .HasColumnName("district_code")
                    .HasMaxLength(100);

            builder.Property(e => e.LocalLevelCode)
                    .HasColumnName("local_level_code")
                    .HasMaxLength(100);

            builder.Property(e => e.WardNo)
                    .HasColumnName("ward_no");

            builder.Property(e => e.PhotoPath)
                    .HasColumnName("photo_path")
                    .HasMaxLength(500);

            // Teaching-specific fields (2026-08-06, ported from the removed Teacher entity/table
            // -- same column names/lengths as the old dbo.teachers table had).
            builder.Property(e => e.TeachingLicenseNo)
                    .HasColumnName("teaching_license_no")
                    .HasMaxLength(100);

            builder.Property(e => e.ExperienceYears)
                    .HasColumnName("experience_years");

            builder.Property(e => e.Specialization)
                    .HasColumnName("specialization")
                    .HasMaxLength(255);

            // Self-referencing, Restrict like Menu's ParentId (never cascade a self-reference --
            // deleting a manager must not cascade-delete their reports).
            builder.HasOne(e => e.Manager)
                    .WithMany()
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.EmployeeCode)
                    .IsUnique()
                    .HasDatabaseName("ix_employees_employee_code");

            // Unique only when populated -- most employees have no login yet.
            builder.HasIndex(e => e.UserId)
                    .IsUnique()
                    .HasFilter("user_id IS NOT NULL")
                    .HasDatabaseName("ix_employees_user_id");

            builder.HasIndex(e => e.ManagerId)
                    .HasDatabaseName("ix_employees_manager_id");
        }
    }
}
