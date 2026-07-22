using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedfeegenerationlogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "carried_forward_amount",
                schema: "dbo",
                table: "fee_invoices",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "fee_generation_runs",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_year = table.Column<int>(type: "integer", nullable: false),
                    billing_month = table.Column<int>(type: "integer", nullable: false),
                    generated_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_regenerated_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deleted_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_generation_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_generation_runs_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalSchema: "dbo",
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fee_generation_runs_is_deleted",
                schema: "dbo",
                table: "fee_generation_runs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_fee_generation_runs_year_period",
                schema: "dbo",
                table: "fee_generation_runs",
                columns: new[] { "academic_year_id", "billing_year", "billing_month" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fee_generation_runs",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "carried_forward_amount",
                schema: "dbo",
                table: "fee_invoices");
        }
    }
}
