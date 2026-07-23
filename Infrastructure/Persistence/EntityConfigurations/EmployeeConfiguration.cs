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

            builder.HasIndex(e => e.EmployeeCode)
                    .IsUnique()
                    .HasDatabaseName("ix_employees_employee_code");

            // Unique only when populated -- most employees have no login yet.
            builder.HasIndex(e => e.UserId)
                    .IsUnique()
                    .HasFilter("user_id IS NOT NULL")
                    .HasDatabaseName("ix_employees_user_id");
        }
    }
}
