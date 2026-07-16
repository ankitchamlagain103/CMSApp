using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeLoanConfiguration : SoftDeleteAuditableEntityConfiguration<EmployeeLoan>
    {
        public override void Configure(EntityTypeBuilder<EmployeeLoan> builder)
        {
            base.Configure(builder);

            builder.ToTable("employee_loans", "dbo");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                    .HasColumnName("id");

            builder.Property(l => l.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.DeductionType), restricted in the service to
            // Domain/Constants/LoanTypeCodes (LOAN/ADVANCE) rather than any DeductionType code.
            builder.Property(l => l.LoanTypeCode)
                    .HasColumnName("loan_type_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(l => l.PrincipalAmount)
                    .HasColumnName("principal_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(l => l.EmiAmount)
                    .HasColumnName("emi_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(l => l.RequestedDate)
                    .HasColumnName("requested_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(l => l.StartDate)
                    .HasColumnName("start_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(l => l.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.Property(l => l.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(l => l.Employee)
                    .WithMany(e => e.Loans)
                    .HasForeignKey(l => l.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(l => l.EmployeeId)
                    .HasDatabaseName("ix_employee_loans_employee_id");
        }
    }
}
