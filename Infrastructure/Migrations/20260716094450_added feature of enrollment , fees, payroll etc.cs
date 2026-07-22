using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedfeatureofenrollmentfeespayrolletc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_loans",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    emi_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    requested_date = table.Column<DateTime>(type: "date", nullable: false),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_employee_loans", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_loans_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_adjustments",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_year = table.Column<int>(type: "integer", nullable: false),
                    billing_month = table.Column<int>(type: "integer", nullable: false),
                    adjustment_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    applied_fee_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_fee_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_adjustments_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_invoices",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_year = table.Column<int>(type: "integer", nullable: false),
                    billing_month = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    previous_due_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    generated_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_fee_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_invoices_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalSchema: "dbo",
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fee_invoices_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_payments",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    payment_mode = table.Column<int>(type: "integer", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_fee_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_payments_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_rules",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rule_type = table.Column<int>(type: "integer", nullable: false),
                    trigger_stage = table.Column<int>(type: "integer", nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    min_months_together = table.Column<int>(type: "integer", nullable: true),
                    days_before_due_date = table.Column<int>(type: "integer", nullable: true),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_category_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_combinable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_fee_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_rules_academic_classes_academic_class_id",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_runs",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    month_index = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    generated_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    paid_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_payroll_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_payroll_runs_fiscal_years_fiscal_year_id",
                        column: x => x.fiscal_year_id,
                        principalSchema: "dbo",
                        principalTable: "fiscal_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "salary_adjustments",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    month_index = table.Column<int>(type: "integer", nullable: false),
                    adjustment_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    applied_salary_slip_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_salary_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "FK_salary_adjustments_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salary_adjustments_fiscal_years_fiscal_year_id",
                        column: x => x.fiscal_year_id,
                        principalSchema: "dbo",
                        principalTable: "fiscal_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fee_invoice_lines",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    fee_structure_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_discount_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_scholarship_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_adjustment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_category_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_invoice_lines_fee_invoices_fee_invoice_id",
                        column: x => x.fee_invoice_id,
                        principalSchema: "dbo",
                        principalTable: "fee_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fee_payment_allocations",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_payment_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_payment_allocations_fee_invoices_fee_invoice_id",
                        column: x => x.fee_invoice_id,
                        principalSchema: "dbo",
                        principalTable: "fee_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fee_payment_allocations_fee_payments_fee_payment_id",
                        column: x => x.fee_payment_id,
                        principalSchema: "dbo",
                        principalTable: "fee_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "salary_slips",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slip_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_salary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    period_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    month_days = table.Column<int>(type: "integer", nullable: false),
                    pay_days = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    unpaid_leave_days = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    gross_earnings = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_deductions = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    net_pay = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
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
                    table.PrimaryKey("PK_salary_slips", x => x.id);
                    table.ForeignKey(
                        name: "FK_salary_slips_employee_salaries_employee_salary_id",
                        column: x => x.employee_salary_id,
                        principalSchema: "dbo",
                        principalTable: "employee_salaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salary_slips_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salary_slips_payroll_runs_payroll_run_id",
                        column: x => x.payroll_run_id,
                        principalSchema: "dbo",
                        principalTable: "payroll_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "salary_slip_lines",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_slip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_type = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    component_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    salary_adjustment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_loan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_slip_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_salary_slip_lines_salary_slips_salary_slip_id",
                        column: x => x.salary_slip_id,
                        principalSchema: "dbo",
                        principalTable: "salary_slips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_loans_employee_id",
                schema: "dbo",
                table: "employee_loans",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loans_is_deleted",
                schema: "dbo",
                table: "employee_loans",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_fee_adjustments_enrollment_period_status",
                schema: "dbo",
                table: "fee_adjustments",
                columns: new[] { "enrollment_id", "billing_year", "billing_month", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_fee_adjustments_is_deleted",
                schema: "dbo",
                table: "fee_adjustments",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_fee_invoice_lines_invoice",
                schema: "dbo",
                table: "fee_invoice_lines",
                column: "fee_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_invoices_enrollment_period",
                schema: "dbo",
                table: "fee_invoices",
                columns: new[] { "enrollment_id", "billing_year", "billing_month" },
                unique: true,
                filter: "status <> 6 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_fee_invoices_enrollment_status",
                schema: "dbo",
                table: "fee_invoices",
                columns: new[] { "enrollment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_fee_invoices_invoice_no",
                schema: "dbo",
                table: "fee_invoices",
                column: "invoice_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_invoices_is_deleted",
                schema: "dbo",
                table: "fee_invoices",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_fee_invoices_year_period",
                schema: "dbo",
                table: "fee_invoices",
                columns: new[] { "academic_year_id", "billing_year", "billing_month" });

            migrationBuilder.CreateIndex(
                name: "ix_fee_payment_allocations_invoice",
                schema: "dbo",
                table: "fee_payment_allocations",
                column: "fee_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_payment_allocations_payment_invoice",
                schema: "dbo",
                table: "fee_payment_allocations",
                columns: new[] { "fee_payment_id", "fee_invoice_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fee_payments_enrollment_date",
                schema: "dbo",
                table: "fee_payments",
                columns: new[] { "enrollment_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "IX_fee_payments_is_deleted",
                schema: "dbo",
                table: "fee_payments",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_fee_payments_receipt_no",
                schema: "dbo",
                table: "fee_payments",
                column: "receipt_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_rules_academic_class_id",
                schema: "dbo",
                table: "fee_rules",
                column: "academic_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_fee_rules_code",
                schema: "dbo",
                table: "fee_rules",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_rules_is_deleted",
                schema: "dbo",
                table: "fee_rules",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_fiscal_month",
                schema: "dbo",
                table: "payroll_runs",
                columns: new[] { "fiscal_year_id", "month_index" },
                unique: true,
                filter: "status <> 4 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_runs_is_deleted",
                schema: "dbo",
                table: "payroll_runs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_salary_adjustments_employee_period_status",
                schema: "dbo",
                table: "salary_adjustments",
                columns: new[] { "employee_id", "fiscal_year_id", "month_index", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_salary_adjustments_fiscal_year_id",
                schema: "dbo",
                table: "salary_adjustments",
                column: "fiscal_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_salary_adjustments_is_deleted",
                schema: "dbo",
                table: "salary_adjustments",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_salary_slip_lines_slip",
                schema: "dbo",
                table: "salary_slip_lines",
                column: "salary_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_salary_slips_employee",
                schema: "dbo",
                table: "salary_slips",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_salary_slips_employee_salary_id",
                schema: "dbo",
                table: "salary_slips",
                column: "employee_salary_id");

            migrationBuilder.CreateIndex(
                name: "IX_salary_slips_is_deleted",
                schema: "dbo",
                table: "salary_slips",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_salary_slips_run_employee",
                schema: "dbo",
                table: "salary_slips",
                columns: new[] { "payroll_run_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_salary_slips_slip_no",
                schema: "dbo",
                table: "salary_slips",
                column: "slip_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_loans",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fee_adjustments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fee_invoice_lines",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fee_payment_allocations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fee_rules",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "salary_adjustments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "salary_slip_lines",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fee_invoices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fee_payments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "salary_slips",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "payroll_runs",
                schema: "dbo");
        }
    }
}
