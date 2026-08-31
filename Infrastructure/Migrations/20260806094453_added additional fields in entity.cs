using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedadditionalfieldsinentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teacher_assignments_teachers_teacher_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropTable(
                name: "teachers",
                schema: "dbo");

            migrationBuilder.AddColumn<int>(
                name: "experience_years",
                schema: "dbo",
                table: "employees",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialization",
                schema: "dbo",
                table: "employees",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "teaching_license_no",
                schema: "dbo",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_teacher_assignments_employees_teacher_id",
                schema: "dbo",
                table: "teacher_assignments",
                column: "teacher_id",
                principalSchema: "dbo",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teacher_assignments_employees_teacher_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropColumn(
                name: "experience_years",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "specialization",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "teaching_license_no",
                schema: "dbo",
                table: "employees");

            migrationBuilder.CreateTable(
                name: "teachers",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    experience_years = table.Column<int>(type: "integer", nullable: true),
                    specialization = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    teaching_license_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teachers", x => x.id);
                    table.ForeignKey(
                        name: "FK_teachers_employees_id",
                        column: x => x.id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_teacher_assignments_teachers_teacher_id",
                schema: "dbo",
                table: "teacher_assignments",
                column: "teacher_id",
                principalSchema: "dbo",
                principalTable: "teachers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
