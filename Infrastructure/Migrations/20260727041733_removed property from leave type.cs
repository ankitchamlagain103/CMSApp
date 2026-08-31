using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removedpropertyfromleavetype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_consecutive_days",
                schema: "dbo",
                table: "leave_types",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_days_per_month",
                schema: "dbo",
                table: "leave_types",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_days_per_week",
                schema: "dbo",
                table: "leave_types",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_emergency",
                schema: "dbo",
                table: "leave_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_consecutive_days",
                schema: "dbo",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "max_days_per_month",
                schema: "dbo",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "max_days_per_week",
                schema: "dbo",
                table: "leave_types");

            migrationBuilder.DropColumn(
                name: "is_emergency",
                schema: "dbo",
                table: "leave_requests");
        }
    }
}
