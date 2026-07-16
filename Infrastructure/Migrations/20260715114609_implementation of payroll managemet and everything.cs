using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class implementationofpayrollmanagemetandeverything : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teacher_salaries",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "ix_teachers_employee_no",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropIndex(
                name: "IX_teachers_is_deleted",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropIndex(
                name: "ix_fee_structures_academic_class_id",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "deleted_ts",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "employee_no",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "joining_date",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "phone",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.RenameColumn(
                name: "middle_name",
                schema: "dbo",
                table: "teachers",
                newName: "teaching_license_no");

            migrationBuilder.RenameColumn(
                name: "email",
                schema: "dbo",
                table: "teachers",
                newName: "specialization");

            migrationBuilder.RenameColumn(
                name: "monthly_fee_amount",
                schema: "dbo",
                table: "fee_structures",
                newName: "amount");

            migrationBuilder.AddColumn<int>(
                name: "experience_years",
                schema: "dbo",
                table: "teachers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "retirement_exemption_cap_amount",
                schema: "dbo",
                table: "fiscal_years",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "fee_category_code",
                schema: "dbo",
                table: "fee_structures",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "frequency_type",
                schema: "dbo",
                table: "fee_structures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_optional",
                schema: "dbo",
                table: "fee_structures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_refundable",
                schema: "dbo",
                table: "fee_structures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "document_templates",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "date", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    join_date = table.Column<DateTime>(type: "date", nullable: true),
                    employee_category_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    job_position_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    employment_status = table.Column<int>(type: "integer", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_mode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enrollment_fee_selections",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_category_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollment_fee_selections", x => x.id);
                    table.ForeignKey(
                        name: "FK_enrollment_fee_selections_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_salaries",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from_date = table.Column<DateTime>(type: "date", nullable: false),
                    assessment_type = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_employee_salaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_salaries_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_insurance_premiums",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_salary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    insurance_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    annual_premium_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_insurance_premiums", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_insurance_premiums_employee_salaries_employee_sala~",
                        column: x => x.employee_salary_id,
                        principalSchema: "dbo",
                        principalTable: "employee_salaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_salary_components",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_salary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    frequency_type = table.Column<int>(type: "integer", nullable: false),
                    is_taxable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_retirement_contribution = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_salary_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_salary_components_employee_salaries_employee_salar~",
                        column: x => x.employee_salary_id,
                        principalSchema: "dbo",
                        principalTable: "employee_salaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_salary_deductions",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_salary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deduction_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    frequency_type = table.Column<int>(type: "integer", nullable: false),
                    is_retirement_contribution = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_salary_deductions", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_salary_deductions_employee_salaries_employee_salar~",
                        column: x => x.employee_salary_id,
                        principalSchema: "dbo",
                        principalTable: "employee_salaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fee_structures_class_category",
                schema: "dbo",
                table: "fee_structures",
                columns: new[] { "academic_class_id", "fee_category_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_templates_template_type",
                schema: "dbo",
                table: "document_templates",
                column: "template_type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_insurance_premiums_salary_type",
                schema: "dbo",
                table: "employee_insurance_premiums",
                columns: new[] { "employee_salary_id", "insurance_type_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_salaries_employee_effective_date",
                schema: "dbo",
                table: "employee_salaries",
                columns: new[] { "employee_id", "effective_from_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_salaries_is_deleted",
                schema: "dbo",
                table: "employee_salaries",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_components_salary_component",
                schema: "dbo",
                table: "employee_salary_components",
                columns: new[] { "employee_salary_id", "component_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_salary_deductions_salary_deduction",
                schema: "dbo",
                table: "employee_salary_deductions",
                columns: new[] { "employee_salary_id", "deduction_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_employee_code",
                schema: "dbo",
                table: "employees",
                column: "employee_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_is_deleted",
                schema: "dbo",
                table: "employees",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_employees_user_id",
                schema: "dbo",
                table: "employees",
                column: "user_id",
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_enrollment_fee_selections_enrollment_category",
                schema: "dbo",
                table: "enrollment_fee_selections",
                columns: new[] { "enrollment_id", "fee_category_code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_teachers_employees_id",
                schema: "dbo",
                table: "teachers",
                column: "id",
                principalSchema: "dbo",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teachers_employees_id",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropTable(
                name: "document_templates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "employee_insurance_premiums",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "employee_salary_components",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "employee_salary_deductions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "enrollment_fee_selections",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "employee_salaries",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "ix_fee_structures_class_category",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "experience_years",
                schema: "dbo",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "retirement_exemption_cap_amount",
                schema: "dbo",
                table: "fiscal_years");

            migrationBuilder.DropColumn(
                name: "fee_category_code",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "frequency_type",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "is_optional",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "is_refundable",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.RenameColumn(
                name: "teaching_license_no",
                schema: "dbo",
                table: "teachers",
                newName: "middle_name");

            migrationBuilder.RenameColumn(
                name: "specialization",
                schema: "dbo",
                table: "teachers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "amount",
                schema: "dbo",
                table: "fee_structures",
                newName: "monthly_fee_amount");

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                schema: "dbo",
                table: "teachers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_ts",
                schema: "dbo",
                table: "teachers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employee_no",
                schema: "dbo",
                table: "teachers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "dbo",
                table: "teachers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "dbo",
                table: "teachers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "joining_date",
                schema: "dbo",
                table: "teachers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "dbo",
                table: "teachers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone",
                schema: "dbo",
                table: "teachers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "dbo",
                table: "teachers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "teacher_salaries",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowances = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    assessment_type = table.Column<int>(type: "integer", nullable: false),
                    basic_salary = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deleted_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    effective_from_date = table.Column<DateTime>(type: "date", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_salaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_teacher_salaries_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "dbo",
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_teachers_employee_no",
                schema: "dbo",
                table: "teachers",
                column: "employee_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teachers_is_deleted",
                schema: "dbo",
                table: "teachers",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_fee_structures_academic_class_id",
                schema: "dbo",
                table: "fee_structures",
                column: "academic_class_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacher_salaries_is_deleted",
                schema: "dbo",
                table: "teacher_salaries",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_salaries_teacher_effective_date",
                schema: "dbo",
                table: "teacher_salaries",
                columns: new[] { "teacher_id", "effective_from_date" },
                unique: true);
        }
    }
}
