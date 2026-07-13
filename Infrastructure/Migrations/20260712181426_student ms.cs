using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class studentms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_years",
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
                    table.PrimaryKey("PK_academic_years", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guardians",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    occupation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_guardians", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "date", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    admission_date = table.Column<DateTime>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_students", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teachers",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    joining_date = table.Column<DateTime>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_teachers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "academic_classes",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    section_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_academic_classes", x => x.id);
                    table.ForeignKey(
                        name: "FK_academic_classes_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalSchema: "dbo",
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_guardians",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_guardians", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_guardians_guardians_guardian_id",
                        column: x => x.guardian_id,
                        principalSchema: "dbo",
                        principalTable: "guardians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_guardians_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "dbo",
                        principalTable: "students",
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
                    table.PrimaryKey("PK_teacher_qualifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_teacher_qualifications_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "dbo",
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class_subjects",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_subjects", x => x.id);
                    table.ForeignKey(
                        name: "FK_class_subjects_academic_classes_academic_class_id",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roll_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    enrollment_date = table.Column<DateTime>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "FK_enrollments_academic_classes_academic_class_id",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "dbo",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_assignments",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_class_teacher = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_teacher_assignments_class_subjects_class_subject_id",
                        column: x => x.class_subject_id,
                        principalSchema: "dbo",
                        principalTable: "class_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teacher_assignments_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalSchema: "dbo",
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enrollment_subjects",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollment_subjects", x => x.id);
                    table.ForeignKey(
                        name: "FK_enrollment_subjects_class_subjects_class_subject_id",
                        column: x => x.class_subject_id,
                        principalSchema: "dbo",
                        principalTable: "class_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollment_subjects_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "dbo",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_academic_classes_is_deleted",
                schema: "dbo",
                table: "academic_classes",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_academic_classes_year_grade_section",
                schema: "dbo",
                table: "academic_classes",
                columns: new[] { "academic_year_id", "grade_code", "section_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_code",
                schema: "dbo",
                table: "academic_years",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_is_deleted",
                schema: "dbo",
                table: "academic_years",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_class_subjects_class_subject",
                schema: "dbo",
                table: "class_subjects",
                columns: new[] { "academic_class_id", "subject_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_subjects_class_subject_id",
                schema: "dbo",
                table: "enrollment_subjects",
                column: "class_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_enrollment_subjects_enrollment_class_subject",
                schema: "dbo",
                table: "enrollment_subjects",
                columns: new[] { "enrollment_id", "class_subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_academic_class_id",
                schema: "dbo",
                table: "enrollments",
                column: "academic_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_is_deleted",
                schema: "dbo",
                table: "enrollments",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_student_class",
                schema: "dbo",
                table: "enrollments",
                columns: new[] { "student_id", "academic_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guardians_is_deleted",
                schema: "dbo",
                table: "guardians",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_student_guardians_guardian_id",
                schema: "dbo",
                table: "student_guardians",
                column: "guardian_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_guardians_student_guardian",
                schema: "dbo",
                table: "student_guardians",
                columns: new[] { "student_id", "guardian_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_students_admission_no",
                schema: "dbo",
                table: "students",
                column: "admission_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_is_deleted",
                schema: "dbo",
                table: "students",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_assignments_class_subject_id",
                schema: "dbo",
                table: "teacher_assignments",
                column: "class_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_assignments_teacher_class_subject",
                schema: "dbo",
                table: "teacher_assignments",
                columns: new[] { "teacher_id", "class_subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teacher_qualifications_teacher_id",
                schema: "dbo",
                table: "teacher_qualifications",
                column: "teacher_id");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enrollment_subjects",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "student_guardians",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "teacher_assignments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "teacher_qualifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "enrollments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "guardians",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "class_subjects",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "teachers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "students",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "academic_classes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "academic_years",
                schema: "dbo");
        }
    }
}
