using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class additionalentityupdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "credit_hours",
                schema: "dbo",
                table: "class_subjects",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "full_marks",
                schema: "dbo",
                table: "class_subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pass_marks",
                schema: "dbo",
                table: "class_subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "practical_marks",
                schema: "dbo",
                table: "class_subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "theory_marks",
                schema: "dbo",
                table: "class_subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fee_structures",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monthly_fee_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_fee_structures", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_structures_academic_classes_academic_class_id",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_years",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_fiscal_years", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "student_discounts",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
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
                    table.PrimaryKey("PK_student_discounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_discounts_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_scholarships",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scholarship_type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
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
                    table.PrimaryKey("PK_student_scholarships", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_scholarships_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_salaries",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from_date = table.Column<DateTime>(type: "date", nullable: false),
                    basic_salary = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    allowances = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
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
                    table.PrimaryKey("PK_teacher_salaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_teacher_salaries_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "dbo",
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_slabs",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_type = table.Column<int>(type: "integer", nullable: false),
                    min_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    max_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    tax_rate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    slab_order = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_slabs", x => x.id);
                    table.CheckConstraint("ck_tax_slabs_amount_range", "max_amount IS NULL OR max_amount > min_amount");
                    table.ForeignKey(
                        name: "FK_tax_slabs_fiscal_years_fiscal_year_id",
                        column: x => x.fiscal_year_id,
                        principalSchema: "dbo",
                        principalTable: "fiscal_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_class_subjects_marks_range",
                schema: "dbo",
                table: "class_subjects",
                sql: "pass_marks IS NULL OR full_marks IS NULL OR pass_marks <= full_marks");

            migrationBuilder.CreateIndex(
                name: "ix_fee_structures_academic_class_id",
                schema: "dbo",
                table: "fee_structures",
                column: "academic_class_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fee_structures_is_deleted",
                schema: "dbo",
                table: "fee_structures",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_fiscal_years_code",
                schema: "dbo",
                table: "fiscal_years",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_years_is_deleted",
                schema: "dbo",
                table: "fiscal_years",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_student_discounts_enrollment_id",
                schema: "dbo",
                table: "student_discounts",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_discounts_is_deleted",
                schema: "dbo",
                table: "student_discounts",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_student_scholarships_enrollment_id",
                schema: "dbo",
                table: "student_scholarships",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_scholarships_is_deleted",
                schema: "dbo",
                table: "student_scholarships",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_tax_slabs_year_assessment_order",
                schema: "dbo",
                table: "tax_slabs",
                columns: new[] { "fiscal_year_id", "assessment_type", "slab_order" },
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fee_structures",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "student_discounts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "student_scholarships",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "tax_slabs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "teacher_salaries",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "fiscal_years",
                schema: "dbo");

            migrationBuilder.DropCheckConstraint(
                name: "ck_class_subjects_marks_range",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "credit_hours",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "full_marks",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "pass_marks",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "practical_marks",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "theory_marks",
                schema: "dbo",
                table: "class_subjects");
        }
    }
}
