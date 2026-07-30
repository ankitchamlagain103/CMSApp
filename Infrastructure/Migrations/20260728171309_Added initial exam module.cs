using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedinitialexammodule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_practical",
                schema: "dbo",
                table: "class_subjects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_theory",
                schema: "dbo",
                table: "class_subjects",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "practical_pass_marks",
                schema: "dbo",
                table: "class_subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "theory_pass_marks",
                schema: "dbo",
                table: "class_subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exam_terms",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: false),
                    publish_result = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_exam_terms", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_terms_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalSchema: "dbo",
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grade_scales",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    min_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    max_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    grade_point = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_grade_scales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "student_promotions",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_date = table.Column<DateTime>(type: "date", nullable: false),
                    promotion_type = table.Column<int>(type: "integer", nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_promotions", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_promotions_enrollments_from_enrollment_id",
                        column: x => x.from_enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_promotions_enrollments_to_enrollment_id",
                        column: x => x.to_enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_promotions_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "dbo",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exams",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    weightage_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_final_exam = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exams", x => x.id);
                    table.ForeignKey(
                        name: "FK_exams_exam_terms_exam_term_id",
                        column: x => x.exam_term_id,
                        principalSchema: "dbo",
                        principalTable: "exam_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_results",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_marks = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    obtained_marks = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    gpa = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: true),
                    result_status = table.Column<int>(type: "integer", nullable: false),
                    published_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_student_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_results_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_results_exam_terms_exam_term_id",
                        column: x => x.exam_term_id,
                        principalSchema: "dbo",
                        principalTable: "exam_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exam_schedules",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_date = table.Column<DateTime>(type: "date", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    room = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    invigilator_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    maximum_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    pass_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    calendar_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    marks_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_schedules_class_sections_class_section_id",
                        column: x => x.class_section_id,
                        principalSchema: "dbo",
                        principalTable: "class_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_schedules_class_subjects_class_subject_id",
                        column: x => x.class_subject_id,
                        principalSchema: "dbo",
                        principalTable: "class_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_schedules_employees_invigilator_employee_id",
                        column: x => x.invigilator_employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_schedules_exams_exam_id",
                        column: x => x.exam_id,
                        principalSchema: "dbo",
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_exam_marks",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    theory_obtained_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    practical_obtained_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    internal_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    theory_grace_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    practical_grace_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    theory_absent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    practical_absent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    total_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    grade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    grade_point = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    remarks = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_absent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_exam_marks", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_exam_marks_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_exam_marks_exam_schedules_exam_schedule_id",
                        column: x => x.exam_schedule_id,
                        principalSchema: "dbo",
                        principalTable: "exam_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_class_subjects_practical_marks_range",
                schema: "dbo",
                table: "class_subjects",
                sql: "practical_pass_marks IS NULL OR practical_marks IS NULL OR practical_pass_marks <= practical_marks");

            migrationBuilder.AddCheckConstraint(
                name: "ck_class_subjects_theory_marks_range",
                schema: "dbo",
                table: "class_subjects",
                sql: "theory_pass_marks IS NULL OR theory_marks IS NULL OR theory_pass_marks <= theory_marks");

            migrationBuilder.CreateIndex(
                name: "IX_exam_schedules_class_section_id",
                schema: "dbo",
                table: "exam_schedules",
                column: "class_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_schedules_class_subject_id",
                schema: "dbo",
                table: "exam_schedules",
                column: "class_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_schedules_exam_date",
                schema: "dbo",
                table: "exam_schedules",
                column: "exam_date");

            migrationBuilder.CreateIndex(
                name: "ix_exam_schedules_exam_subject_section",
                schema: "dbo",
                table: "exam_schedules",
                columns: new[] { "exam_id", "class_subject_id", "class_section_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_schedules_invigilator_employee_id",
                schema: "dbo",
                table: "exam_schedules",
                column: "invigilator_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_terms_academic_year_id",
                schema: "dbo",
                table: "exam_terms",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_terms_code",
                schema: "dbo",
                table: "exam_terms",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_terms_is_deleted",
                schema: "dbo",
                table: "exam_terms",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_exams_exam_term_id",
                schema: "dbo",
                table: "exams",
                column: "exam_term_id");

            migrationBuilder.CreateIndex(
                name: "ix_grade_scales_grade",
                schema: "dbo",
                table: "grade_scales",
                column: "grade",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grade_scales_is_deleted",
                schema: "dbo",
                table: "grade_scales",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_student_exam_marks_enrollment_id",
                schema: "dbo",
                table: "student_exam_marks",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_exam_marks_schedule_enrollment",
                schema: "dbo",
                table: "student_exam_marks",
                columns: new[] { "exam_schedule_id", "enrollment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_promotions_from_enrollment_id",
                schema: "dbo",
                table: "student_promotions",
                column: "from_enrollment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_promotions_student_id",
                schema: "dbo",
                table: "student_promotions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_promotions_to_enrollment_id",
                schema: "dbo",
                table: "student_promotions",
                column: "to_enrollment_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_results_enrollment_term",
                schema: "dbo",
                table: "student_results",
                columns: new[] { "enrollment_id", "exam_term_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_results_exam_term_id",
                schema: "dbo",
                table: "student_results",
                column: "exam_term_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_results_is_deleted",
                schema: "dbo",
                table: "student_results",
                column: "is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grade_scales",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "student_exam_marks",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "student_promotions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "student_results",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "exam_schedules",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "exams",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "exam_terms",
                schema: "dbo");

            migrationBuilder.DropCheckConstraint(
                name: "ck_class_subjects_practical_marks_range",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_class_subjects_theory_marks_range",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "has_practical",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "has_theory",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "practical_pass_marks",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "theory_pass_marks",
                schema: "dbo",
                table: "class_subjects");
        }
    }
}
