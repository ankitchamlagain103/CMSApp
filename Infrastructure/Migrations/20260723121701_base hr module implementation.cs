using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class basehrmoduleimplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                schema: "dbo",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "level_code",
                schema: "dbo",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "manager_id",
                schema: "dbo",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_path",
                schema: "dbo",
                table: "employees",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_code",
                schema: "dbo",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                schema: "dbo",
                table: "calendar_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "employee_id",
                schema: "dbo",
                table: "calendar_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_code",
                schema: "dbo",
                table: "calendar_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "student_id",
                schema: "dbo",
                table: "calendar_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "leave_types",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    days_per_year = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    carry_forward = table.Column<bool>(type: "boolean", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_leave_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_leave_balances",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    used = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    pending = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_leave_balances", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_leave_balances_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_leave_balances_fiscal_years_fiscal_year_id",
                        column: x => x.fiscal_year_id,
                        principalSchema: "dbo",
                        principalTable: "fiscal_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_leave_balances_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalSchema: "dbo",
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateTime>(type: "date", nullable: false),
                    to_date = table.Column<DateTime>(type: "date", nullable: false),
                    days = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    substitute_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manager_status = table.Column<int>(type: "integer", nullable: false),
                    manager_remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    manager_decision_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    manager_decision_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    hr_status = table.Column<int>(type: "integer", nullable: false),
                    hr_remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hr_decision_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hr_decision_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    attachment_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    attachment_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    attachment_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_requests_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leave_requests_employees_substitute_employee_id",
                        column: x => x.substitute_employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leave_requests_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalSchema: "dbo",
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_substitutes",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responsibility = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_substitutes", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_substitutes_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leave_substitutes_leave_requests_leave_request_id",
                        column: x => x.leave_request_id,
                        principalSchema: "dbo",
                        principalTable: "leave_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employees_manager_id",
                schema: "dbo",
                table: "employees",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_employee_id",
                schema: "dbo",
                table: "calendar_events",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_student_id",
                schema: "dbo",
                table: "calendar_events",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_leave_balances_employee_type_year",
                schema: "dbo",
                table: "employee_leave_balances",
                columns: new[] { "employee_id", "leave_type_id", "fiscal_year_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_balances_fiscal_year_id",
                schema: "dbo",
                table: "employee_leave_balances",
                column: "fiscal_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_balances_leave_type_id",
                schema: "dbo",
                table: "employee_leave_balances",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_date_range",
                schema: "dbo",
                table: "leave_requests",
                columns: new[] { "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_employee_id",
                schema: "dbo",
                table: "leave_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_is_deleted",
                schema: "dbo",
                table: "leave_requests",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_leave_type_id",
                schema: "dbo",
                table: "leave_requests",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_substitute_employee_id",
                schema: "dbo",
                table: "leave_requests",
                column: "substitute_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_substitutes_employee_id",
                schema: "dbo",
                table: "leave_substitutes",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_substitutes_leave_request_id",
                schema: "dbo",
                table: "leave_substitutes",
                column: "leave_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_types_is_deleted",
                schema: "dbo",
                table: "leave_types",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_leave_types_name",
                schema: "dbo",
                table: "leave_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_employee_id_is_read",
                schema: "dbo",
                table: "notifications",
                columns: new[] { "employee_id", "is_read" });

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_events_employees_employee_id",
                schema: "dbo",
                table: "calendar_events",
                column: "employee_id",
                principalSchema: "dbo",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_events_students_student_id",
                schema: "dbo",
                table: "calendar_events",
                column: "student_id",
                principalSchema: "dbo",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_employees_manager_id",
                schema: "dbo",
                table: "employees",
                column: "manager_id",
                principalSchema: "dbo",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_calendar_events_employees_employee_id",
                schema: "dbo",
                table: "calendar_events");

            migrationBuilder.DropForeignKey(
                name: "FK_calendar_events_students_student_id",
                schema: "dbo",
                table: "calendar_events");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_employees_manager_id",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropTable(
                name: "employee_leave_balances",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "leave_substitutes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "leave_requests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "leave_types",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "ix_employees_manager_id",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_calendar_events_employee_id",
                schema: "dbo",
                table: "calendar_events");

            migrationBuilder.DropIndex(
                name: "ix_calendar_events_student_id",
                schema: "dbo",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "branch_code",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "level_code",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "manager_id",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "photo_path",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "province_code",
                schema: "dbo",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "branch_code",
                schema: "dbo",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "employee_id",
                schema: "dbo",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "province_code",
                schema: "dbo",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "student_id",
                schema: "dbo",
                table: "calendar_events");
        }
    }
}
