using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedupdate1exammodule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_exam_marks_exam_schedules_exam_schedule_id",
                schema: "dbo",
                table: "student_exam_marks");

            migrationBuilder.DropTable(
                name: "exam_schedules",
                schema: "dbo");

            migrationBuilder.RenameColumn(
                name: "exam_schedule_id",
                schema: "dbo",
                table: "student_exam_marks",
                newName: "exam_id");

            migrationBuilder.RenameIndex(
                name: "ix_student_exam_marks_schedule_enrollment",
                schema: "dbo",
                table: "student_exam_marks",
                newName: "ix_student_exam_marks_exam_enrollment");

            migrationBuilder.AddColumn<Guid>(
                name: "calendar_event_id",
                schema: "dbo",
                table: "exams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "class_section_id",
                schema: "dbo",
                table: "exams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "class_subject_id",
                schema: "dbo",
                table: "exams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "end_time",
                schema: "dbo",
                table: "exams",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "exam_date",
                schema: "dbo",
                table: "exams",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "invigilator_employee_id",
                schema: "dbo",
                table: "exams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "marks_locked",
                schema: "dbo",
                table: "exams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "room",
                schema: "dbo",
                table: "exams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "start_time",
                schema: "dbo",
                table: "exams",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_exams_class_section_id",
                schema: "dbo",
                table: "exams",
                column: "class_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_exams_class_subject_id",
                schema: "dbo",
                table: "exams",
                column: "class_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_exams_exam_date",
                schema: "dbo",
                table: "exams",
                column: "exam_date");

            migrationBuilder.CreateIndex(
                name: "IX_exams_invigilator_employee_id",
                schema: "dbo",
                table: "exams",
                column: "invigilator_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_exams_term_section_subject_name",
                schema: "dbo",
                table: "exams",
                columns: new[] { "exam_term_id", "class_section_id", "class_subject_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_exams_class_sections_class_section_id",
                schema: "dbo",
                table: "exams",
                column: "class_section_id",
                principalSchema: "dbo",
                principalTable: "class_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_exams_class_subjects_class_subject_id",
                schema: "dbo",
                table: "exams",
                column: "class_subject_id",
                principalSchema: "dbo",
                principalTable: "class_subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_exams_employees_invigilator_employee_id",
                schema: "dbo",
                table: "exams",
                column: "invigilator_employee_id",
                principalSchema: "dbo",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_student_exam_marks_exams_exam_id",
                schema: "dbo",
                table: "student_exam_marks",
                column: "exam_id",
                principalSchema: "dbo",
                principalTable: "exams",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exams_class_sections_class_section_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropForeignKey(
                name: "FK_exams_class_subjects_class_subject_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropForeignKey(
                name: "FK_exams_employees_invigilator_employee_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropForeignKey(
                name: "FK_student_exam_marks_exams_exam_id",
                schema: "dbo",
                table: "student_exam_marks");

            migrationBuilder.DropIndex(
                name: "IX_exams_class_section_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropIndex(
                name: "IX_exams_class_subject_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropIndex(
                name: "ix_exams_exam_date",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropIndex(
                name: "IX_exams_invigilator_employee_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropIndex(
                name: "ix_exams_term_section_subject_name",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "calendar_event_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "class_section_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "class_subject_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "end_time",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "exam_date",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "invigilator_employee_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "marks_locked",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "room",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "start_time",
                schema: "dbo",
                table: "exams");

            migrationBuilder.RenameColumn(
                name: "exam_id",
                schema: "dbo",
                table: "student_exam_marks",
                newName: "exam_schedule_id");

            migrationBuilder.RenameIndex(
                name: "ix_student_exam_marks_exam_enrollment",
                schema: "dbo",
                table: "student_exam_marks",
                newName: "ix_student_exam_marks_schedule_enrollment");

            migrationBuilder.CreateTable(
                name: "exam_schedules",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invigilator_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    calendar_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    exam_date = table.Column<DateTime>(type: "date", nullable: false),
                    marks_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    maximum_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    pass_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    room = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
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

            migrationBuilder.AddForeignKey(
                name: "FK_student_exam_marks_exam_schedules_exam_schedule_id",
                schema: "dbo",
                table: "student_exam_marks",
                column: "exam_schedule_id",
                principalSchema: "dbo",
                principalTable: "exam_schedules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
