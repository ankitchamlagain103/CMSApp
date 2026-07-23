using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedsometableofteacherspecifictoemployeespecific : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teacher_documents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "teacher_qualifications",
                schema: "dbo");

            migrationBuilder.AddColumn<string>(
                name: "cit_number",
                schema: "dbo",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gratuity_number",
                schema: "dbo",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pan_number",
                schema: "dbo",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provident_fund_number",
                schema: "dbo",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ssf_number",
                schema: "dbo",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_documents",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    document_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    valid_until = table.Column<DateTime>(type: "date", nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_documents_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_qualifications",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qualification_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    course_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    institution = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    completion_year = table.Column<int>(type: "integer", nullable: true),
                    score = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_qualifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_qualifications_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_documents_employee_id",
                schema: "dbo",
                table: "employee_documents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_qualifications_employee_id",
                schema: "dbo",
                table: "employee_qualifications",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_documents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "employee_qualifications",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "cit_number",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "gratuity_number",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "pan_number",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "provident_fund_number",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ssf_number",
                schema: "dbo",
                table: "employees");

            migrationBuilder.CreateTable(
                name: "teacher_documents",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    document_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    document_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_until = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_teacher_documents_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "dbo",
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_qualifications",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completion_year = table.Column<int>(type: "integer", nullable: true),
                    course_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    institution = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    qualification_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    score = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_qualifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_teacher_qualifications_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "dbo",
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_teacher_documents_teacher_id",
                schema: "dbo",
                table: "teacher_documents",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_qualifications_teacher_id",
                schema: "dbo",
                table: "teacher_qualifications",
                column: "teacher_id");
        }
    }
}
