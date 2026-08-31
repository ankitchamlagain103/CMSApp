using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedemployeestabletoaddaddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "district_code",
                schema: "dbo",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "local_level_code",
                schema: "dbo",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ward_no",
                schema: "dbo",
                table: "employees",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "district_code",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "local_level_code",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ward_no",
                schema: "dbo",
                table: "employees");
        }
    }
}
