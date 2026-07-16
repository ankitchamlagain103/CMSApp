using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class TaxSlabConfiguration : AuditableEntityConfiguration<TaxSlab>
    {
        public override void Configure(EntityTypeBuilder<TaxSlab> builder)
        {
            base.Configure(builder);

            builder.ToTable("tax_slabs", "dbo", t => t.HasCheckConstraint("ck_tax_slabs_amount_range", "max_amount IS NULL OR max_amount > min_amount"));

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.FiscalYearId)
                    .HasColumnName("fiscal_year_id")
                    .IsRequired();

            builder.Property(s => s.AssessmentType)
                    .HasColumnName("assessment_type")
                    .IsRequired();

            builder.Property(s => s.MinAmount)
                    .HasColumnName("min_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            // Null = no upper bound (the top slab).
            builder.Property(s => s.MaxAmount)
                    .HasColumnName("max_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired(false);

            builder.Property(s => s.TaxRate)
                    .HasColumnName("tax_rate")
                    .HasColumnType("decimal(5,4)")
                    .IsRequired();

            builder.Property(s => s.SlabOrder)
                    .HasColumnName("slab_order")
                    .IsRequired();

            builder.HasOne(s => s.FiscalYear)
                    .WithMany(y => y.TaxSlabs)
                    .HasForeignKey(s => s.FiscalYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.FiscalYearId, s.AssessmentType, s.SlabOrder })
                    .IsUnique()
                    .HasDatabaseName("ix_tax_slabs_year_assessment_order");
        }
    }
}
